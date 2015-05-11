That's my fourth stab at implementing an AST (the first was for [Rapture](http://code.google.com/p/rapt/), the second one was for [Relinq](http://code.google.com/p/relinq/) and the third one was for [Elf](http://code.google.com/p/elf4b/)) and in comparison with previous attempts I like it a lot. Quite heavy work with the AST that I needed to do internally within the decompiler and externally inside my GPGPU project didn't leave me disappointed! Below you can find some key notes about the design of AST data structures and supporting infrastructure.

Some metatheoretic notes. Firstly, this information is quite generic and doesn't touch domain-specific topics that are covered in [HirDesign](HirDesign.md) (HIR stands for high-level intermediate representation) and [HirAndIdioms](HirAndIdioms.md) (which is sort of a technical spec about AST nodes and their possible combinations). Secondly, here I talk mostly about theory with little to no samples. If you prefer to take a look at the code, please, visit the [InAction](InAction.md) wiki page. Finally, this documentation doesn't mean to be comprehensive - it omits many details that can be revealed by looking into the code, but it rather aims to provide something that isn't present in source texts of the library - general ideas and principles behind the implementation.

#### 1. Controlled mutability ####

When working with System.Core expression trees that were in introduced in .NET 3.5 I often felt the necessity to modify the tree slightly (e.g. change a single property of certain node) or to construct a new expression tree gradually (i.e. append some nodes here, some nodes there and, finally, some nodes elsewhere). However, such tasks are pretty much impossible since ETs are immutable and the only way to create them is using static methods of the Expression class. Surely, you can use visitors for that (i.e. recreate the ET clone with applied modifications), but that's often a significant overkill.

So when designing my AST I wanted to achieve maximum flexibility, but, from the other side, was in doubt - what if such flexibility will make it easy to accidentally to corrupt ASTs during regular workflows? However, it seems that my apprehensions were unnecessary. Well, enough theoretical talks, let's see how the stuff works:
  * All nodes feature a variety of constructors, so that when creating a node you can immediately specify some or all of its children. With that you can construct ASTs in functional style familiar to dotnetters from System.Xml.Linq.
  * All nodes can be edited in place (except that you cannot modify lambda's sig and operator's operator type, but there's a good reason for that as described in [HirDesign](HirDesign.md)). With that you can gradually polish your AST without the need to recreate it with visitors - e.g. change invocation style from virtual to non-virtual, adjust a catch clause so that it features a filter and so on.
  * Relationships between nodes can also be changed dynamically, but to prevent accidental corruptions I've added an automatic protection discussed below.
  * Finally, with Freeze and Unfreeze methods you can control mutability of nodes and subtrees of the AST. There's even more - FreezeForever method lets one make the node to be readonly for its entire lifetime, so that noone will accidentally change it. That appeared to be useful for organizing caches.

upd. A tempting idea was to separate read-only and read-write contracts as it's done in CCI (interfaces expose read-only model, while implementing classes expose the read-write model). However, I abandoned it since it's too heavyweight to deal with endless implicit implementations of read-only interfaces, to perform loads of casts between `Seq<INode>` and `Seq<Node>` and to maintain twice as much classes.

#### 2. Parent-children consistency ####

The second principle is put in place to make composing complex ASTs more simple. If you add some node to other node's Children collection, the Parent property of the added node gets updated automatically. And vice versa - changing the Parent property updates Children collection of involved Parent nodes.

However, such approach features a hidden trap. What if you create some node and then decide to add it twice to some other node's Children? That's how I originally discovered the trap - when I decompiled the "dup" instruction I just duplicated the reference to the top node of current evaluation stack and felt fine. However, the following happens in reality:  when you add the node for the first time, everything is okay, but when you add it for the second time the following happens: 1) parent's Children get updated, 2) this triggers update of the node's Parent property, 3) which leads to removing the node from the original Parent. Oh my God - that's not what we wanted to do.

To prevent such glitches, I've implemented a simple rule: if you add some node with non-empty Parent property to Children collection of another node, it gets cloned and a clone is added instead. Surely, this makes one think twice about how to organize complex operations, so that they don't involve unnecessary cloning, but this makes it almost impossible to accidentally corrupt existing data structures.

#### 3. Visitors infrastructure ####

Truesight features three different types of visitors: traversers, reducers and transformers. Traversers visit every node in a tree and, while doing that, populate their internal state. Reducers recursively reduce a tree to a single scalar value of some type (they can be emulated by traversers, but why building all that infrastructure in cases when all the traversal state fit in a single return value?).

Finally, some words about transformers that proved to be the most important particular case of all visitors. They traverse the tree and morph it according to custom rules (e.g. with the use of transformers you can inline all calls into a single method, you can inject certain code around member accesses and so on). Common approach to building transformers (e.g. employed in System.Core) is to recreate the node even if little or no modifications to it have been made. That's how I implemented Truesight as well, however, this point could use some reengineering.

All visitors come in two different flavors - object-oriented and functional. The first flavor is represented by an abstract base class that you can inherit and override methods of interest. This approach proved to be quite heavyweight when all you need is to perform a simple transformation.

To address that, I introduced a different flavor of visitors - a functional one. Functional visitor are composed of several lambdas that are then composed into an object-oriented visitor behind the scene. E.g. to transform certain field accesses in an AST, you can call "ast.Transform((Fld fld) => ...some logic here...". In order to provide a possibility for lambdas to call each other (analogue of calling "this.Foo()" in OO style) and to call default implementations (analogue of calling "base.Bar()" in OO style) I've introduced the "CurrentTransform" and "DefaultTransform" methods that acquire currently active transformer and perform necessary dispatch. For example, to rewrite all static field accesses and leave instance ones untouched you can call "ast.Transform((Fld fld) => fld.This == null ? rewrite(fld) : fld.DefaultTransform()".

#### 4. Visualization ####

I am a strong adept of the opinion that an application should have decent traces for its main data structures and advanced visualization capabilities when appropriate. That's why I've created several visualization features for Truesight. Only textual descriptions are provided here - you can get some pictures from practical experience, e.g. as described at the [InAction](InAction.md) wiki page.

The first one is close-to-c# traces. Every node of an AST can be visualized as text that represents an equivalent snippet of C# code. So basically, you can compose an AST, then call its DumpAsText method and feed the output to csc.exe =) This has some rough edges as described in [TodoDecompiler](TodoDecompiler.md), but works finely in most cases.

The second option of visualization leverages Visual Studio extensibility - debugger type proxies and debugger visualizers. Every subclass of the Node abstract base class has a debugger type proxy that strips off various garbage things and shows only properties that are really relevant to that class. In fact, there are two debugger proxies - one that has the Parent property and one that doesn't have, so that Parents don't litter the view. On the other hand, debugger visualizers are designed to provide more convenient view of an AST, since standard Watch window ain't really the best viewport for complex and branchy AST trees. Unfortunately, at the moment they aren't implemented, so most of the time you'll have to fall back to plain text traces.

Finally, there's another visualization mechanism built into the internals of Truesight. It ain't exposed to the public, but may come in handly if you decide to contribute to the project. Before the flat stream of instructions is decompiled into a nice structured tree, it's stored in an intermediate format - control flow graph. With the help of QuickGraph Truesight provides a way to dump that graph onto disk in plain text format and render it into an image with the use of Graphviz. This is even more convenient. Firstly, executing Truesight.DebuggerVisualizers.exe with the "/reg" argument will register it within Windows shell and associate it as a handler for the ".dotgraph" file type ("/unreg" rolls back the process). Secondly, I've built in a debugger visualizer for control flow graphs that automates these actions - so that by clicking the magnifying glass icon next to the relevant entry in the Watch window you get the graph rendered and opened in your picture viewer.

#### 5. Future work ####

The v+1 of my AST implementations will surely include pattern matching for ASTs since it's a horror to do this manually even for such simple scenarios that are implemented at the moment. Even despite of the fact that the library is written in C# that doesn't provide any built-in tools for organizing pattern matching, this can be implemented as follows (more heavyweight than its F# analogue, but quite tolerable, in my opinion):
```
[AstRewriter(After.Cleanup)]
void MadOptimizationSimple(Node root)
{
    Node a, b, c;
    Ast.Match(root = new Add(a, new Mul(b, c))); // or even "new X(){Prop = value}"
    root.ReplaceWith(new Fma(b, c, a));
}
```

It's also very desirable to support finding templates.  Unlike patterns, described in the previous paragraph, templates perform not only match in depth, but also match in breadth. E.g. suppose you need to find some statement in a block and somewhere below it find another one that satisifies certain criteria - currently you will have to do manual navigations with heaps of auxiliary state that need to be managed.