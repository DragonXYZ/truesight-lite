using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Truesight.Decompiler.Hir;
using Truesight.Decompiler.Hir.Core.ControlFlow;
using Truesight.Decompiler.Hir.Core.Expressions;
using Truesight.Decompiler.Pipeline.Flow.Cfg;
using XenoGears.Functional;
using XenoGears.Assertions;

namespace Truesight.Decompiler.Pipeline.Flow.Scopes
{
    internal class LoopScope : IScope<Loop>
    {
        private readonly BlockScope _parent;
        public IScope Parent { get { return _parent; } }

        Node IScope.Hir { get { return Hir; } }
        public Loop Hir { get { return _loop; } }

        public BaseControlFlowGraph LocalCfg { get { return _parent.LocalCfg; } }
        public BaseControlFlowGraph Body { get { return _body; } }
        public ReadOnlyCollection<Offspring> Offsprings { get { return _offsprings; } }

        public ControlFlowBlock Test { get { return _test; } }
        public ControlFlowBlock Continue { get { return _continue; } }
        public ControlFlowBlock Conv { get { return _conv; } }
        public ReadOnlyCollection<ControlFlowBlock> Pivots { get { return new[] { _continue, _conv }.Where(v => v != null).ToReadOnly(); } }

        private readonly Loop _loop = new Loop();
        private readonly ControlFlowBlock _head; // topologically first vertex in a loop
        private readonly ControlFlowBlock _bot; // topologically last vertex in a loop
        private readonly ControlFlowBlock _conv; // control flow convergence point (not counting in the offsprings)
        private readonly ControlFlowBlock _test; // vertex that represent's loop's test (might be unconditional, e.g. in the case of while-true)
        private readonly ControlFlowBlock _bodyHead; // logically first block in loop's body
        private readonly ControlFlowBlock _bodyBot; // logically last block in loop's body (not including iteration block)
        private readonly ControlFlowBlock _continue; // continue label of a loop (might be null)
        private readonly BaseControlFlowGraph _body; // flow graph that can be decompiled recursively
        private readonly ReadOnlyCollection<Offspring> _offsprings;

        public static LoopScope Decompile(BlockScope parent, ControlFlowBlock head) { return new LoopScope(parent, head); }
        private LoopScope(BlockScope parent, ControlFlowBlock head)
        {
            _parent = parent.AssertNotNull();
            _head = head.AssertNotNull();

            var cfg = LocalCfg;
            var jumpsToHead = cfg.BackVedges(null, _head).Select(e => e.Source).ToReadOnly();
            jumpsToHead.AssertNotEmpty();
            _bot = jumpsToHead.OrderBy(v => cfg.Cflow().IndexOf(v)).Last();
            var cflow = cfg.Cflow(_head, _bot);

            // todo. check this
            //
            // all loops can be divided into four categories:
            // 1) while-do (head is test, either bot or head is continue label),
            // 2) while-true (neither head, nor bot are tests; actually, head is hardcoded to "CS$... = 1"
            //    also either bot or head serve as continue label),
            // 3) do-while (bot is test, it is also the only option for continue
            //    (since here we don't need to separate iteration block from other body instructions, 
            //     and because of (nota bene - never knew it) the fact that continue in do-while 
            //     first tests exit condition and only then proceeds with the next iteration),
            // 4) some loop with offspring embedded into either head or bot
            //
            // in the first two cases before proceeding to detect subscopes and offsprings
            // we need to find out what's the continue label. that's done as follows.
            // todo. we don't process case #4, since that's just too much for me
            //
            // if cflow features multiple jumps to head, then head is a continue label
            // else (if there's only one jump, i.e. from bot), then bot is a continue label
            //
            // now how we distinguish an offspring from loop's body?
            // answer: using regular closure algorithm, i.e.
            // 1) we pick a bot2 vertex (see below for options),
            // 2) we init the closure with cflow(head, bot2) + bot2
            // 3) we expand the closure using universum of parent's local cfg
            // 4) ... and relation of being reachable w/o touching _test and _iter
            //
            // so, bot2 is a logical bottom of the loop's body (while iter exists on its own!)
            // 1) if bot ain't a continue label, then bot2 = bot
            // 2) if bot is a continue label, then check number of jumps to bot
            // 3) if the latter is one, then bot2 = bot
            // 4) else bot2 = the last one that jumps to bot
            //
            // ah, the last point... how do we find convergence?
            // for cases 1 and 3 conv is the node that receives a jump from either head or bot
            // for case 2 conv is the node that receives a jump from the loop's body
            // todo. here we use a hack, assuming that there's exactly 1 jump from the loop's body
            // if there're two or more jumps we will just work incorrectly
            // if there're zero jumps, then it's fail anyways, since we've just entered an infinite loop

            // todo. also verify that test has supported type
            // also don't forget that IL allows using almost everything as a condition of a test
            // cf. brzero === brnull === brfalse, brtrue === brinst

            var hcond = _head.Residue.IsNotEmpty();
            var bcond = _bot.Residue.IsNotEmpty();
            (hcond && bcond).AssertFalse();

            // case #1. while-do
            if (hcond && !bcond)
            {
                _loop.IsWhileDo = true;
                _test = _head;

                _loop.Test = _test.Residue.AssertSingle();
                parent.Hir.AddElements(_test.BalancedCode);

                var next1 = cfg.Vedges(_test, null).First().Target;
                var next2 = cfg.Vedges(_test, null).Second().Target;
                (cflow.Contains(next1) || cflow.Contains(next2)).AssertTrue();
                _conv = cflow.Contains(next1) ? next2 : next1;
                _bodyHead = cflow.Contains(next1) ? next1 : next2;

                var botIsContinue = jumpsToHead.Count() == 1;
                if (botIsContinue)
                {
                    var jumpsToBot = cfg.Vedges(null, _bot).Select(e => e.Source).ToReadOnly();
                    var lastJumpToBot = jumpsToBot.OrderBy(v => cfg.Cflow().IndexOf(v)).Last();

                    _continue = jumpsToBot.Count() > 1 ? _bot : null;
                    _bodyBot = jumpsToBot.Count() > 1 ? lastJumpToBot : _bot;
                }
                else
                {
                    _continue = _head;
                    _bodyBot = _bot;
                }
            }
            // case #2. while-true
            else if (!hcond && !bcond)
            {
                _head.Residue.AssertEmpty();
                var es = _head.BalancedCode.AssertSingle();
                var ass = es.AssertCast<Assign>();
                (ass.Rhs.AssertCast<Const>().Value.AssertCast<int>() == 1).AssertTrue();

                _loop.IsWhileDo = true;
                _loop.Test = new Const(true);
                _test = _head;

                // todo. here we use a hack, assuming that there's exactly 1 jump from the loop's body
                var alienEdges = cfg.AlienEdges(cflow);
                var breaks = alienEdges.Except(cfg.Vedges(null, _head), cfg.Vedges(null, _bot));
                _conv = breaks.Select(e => e.Target).Distinct().AssertSingle();
                _bodyHead = cfg.Vedges(_head, null).AssertSingle().Target;

                var botIsContinue = jumpsToHead.Count() == 1;
                if (botIsContinue)
                {
                    var jumpsToBot = cfg.Vedges(null, _bot).Select(e => e.Source).ToReadOnly();
                    var lastJumpToBot = jumpsToBot.OrderBy(v => cfg.Cflow().IndexOf(v)).Last();

                    _continue = (botIsContinue && jumpsToBot.Count() > 1) ? _bot : null;
                    _bodyBot = (botIsContinue && jumpsToBot.Count() > 1) ? lastJumpToBot : _bot;
                }
                else
                {
                    _continue = _head;
                    _bodyBot = _bot;
                }
            }
            // case #3: do-while
            else if (!hcond && bcond)
            {
                _loop.IsDoWhile = true;
                _test = _bot;

                _loop.Test = _test.Residue.AssertSingle();
                parent.Hir.AddElements(_test.BalancedCode);

                var next1 = cfg.Vedges(_test, null).First().Target;
                var next2 = cfg.Vedges(_test, null).Second().Target;
                (cflow.Contains(next1) || cflow.Contains(next2)).AssertTrue();
                _conv = cflow.Contains(next1) ? next2 : next1;
                _bodyHead = cflow.Contains(next1) ? next1 : next2;

                var jumpsToBot = cfg.Vedges(null, _bot).Select(e => e.Source).ToReadOnly();
                _continue = jumpsToBot.Count() == 1 ? null : _bot;
                _bodyBot = jumpsToBot.OrderBy(v => cfg.Cflow().IndexOf(v)).Last();
            }
            // case #4: mysterious loop
            else
            {
                // todo. we don't process case #4, since that's just too much for me now
                throw AssertionHelper.Fail();
            }

            // here we need to pass a hint to DoDecompileScopesForLoopLocals
            if (_continue != null)
            {
                if (_continue == _test) _loop.Iter.Add(null);
                else _loop.Iter.SetElements(_continue.AssertThat(cfb => cfb.Residue.IsEmpty()).BalancedCode);
            }

            _body = this.InferBody(out _offsprings);
            _loop.Body = BlockScope.Decompile(this, _body).Hir;
        }

        public ViewOfControlFlowGraph InferBody(out ReadOnlyCollection<Offspring> offsprings_out)
        {
            var cfg = LocalCfg;

            var wannabes = cfg.Cflow(_head).Except(cfg.Cflow(_conv));
            wannabes = wannabes.Except(_test, _continue);
            var cflow = cfg.Cflow(_bodyHead, _bodyBot);
            var closure = cflow.Closure(wannabes, (vin, vout) => cfg.Vedge(vout, vin) != null);
            var vertices = closure.OrderBy(v => cfg.Cflow().IndexOf(v)).ToReadOnly();
            vertices.AssertNotEmpty();

            var offsprings = new List<Offspring>();
            var body = cfg.CreateView(vertices, (e, vcfg) =>
            {
                if (vertices.Contains(e.Target))
                {
                    if (_loop.IsWhileDo)
                    {
                        var isWhileTrue = _loop.Test is Const &&
                            _loop.Test.AssertCast<Const>().Value is bool &&
                            (bool)_loop.Test.AssertCast<Const>().Value;
                        if (isWhileTrue)
                        {
                            (e.Source == _test && e.Target == _bodyHead && e.IsUnconditional).AssertTrue();
                            vcfg.AddEigenEdge(new ControlFlowEdge(vcfg.Start, e.Target));
                        }
                        else
                        {
                            (e.Source == _test && e.Target == _bodyHead && e.IsConditional).AssertTrue();
                            vcfg.AddEigenEdge(new ControlFlowEdge(vcfg.Start, e.Target));
                        }
                    }
                    else
                    {
                        (e.Target == _bodyHead).AssertTrue();
                        if (e.Source == _test)
                        {
                            e.IsConditional.AssertTrue();
                            vcfg.AddEigenEdge(new ControlFlowEdge(vcfg.Start, e.Target));
                        }
                    }
                }
                else
                {
                    if (e.Source == _bodyBot && e.Target == (_continue ?? _test))
                    {
                        vcfg.AddEigenEdge(new ControlFlowEdge(e.Source, vcfg.Finish, e.Tag));
                    }
                    else
                    {
                        (cfg.Vedges(e.Source, null).Count() == 2).AssertTrue();
                        offsprings.Add(new Offspring(this, e.Source, e.Target));
                    }
                }
            });
            (body.Start != null && body.Finish != null).AssertTrue();

            offsprings_out = offsprings.ToReadOnly();
            return body;
        }

        public String Uri
        {
            get
            {
                var self = String.Format("loop ({0}..{1} => {2})", _head.Name, _bot.Name, _conv.Name);
                return Parent.Uri + " :: " + self;
            }
        }

        public override String ToString()
        {
            var lite = Uri.Replace("complex :: ", "");
            lite = lite.Replace("body :: ", "");
            return "{" + lite + "}";
        }
    }
}