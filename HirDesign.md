<table><tbody>

<tr><td>HIR stands for high-level intermediate representation and is used in Truesight to distinguish it from the AST concept which is very generic by itself. Consequently, this page covers domain-specific motivations behind designing Truesight nodes. If you'd like to check out generic questions of designing the AST, visit <a href='AstDesign.md'>AstDesign</a>. If you'd like to take a look at the list of nodes and to get an idea how they reflect semantics of high-level programming language such as C#, visit <a href='HirAndIdioms.md'>HirAndIdioms</a>. Finally, you might also be interested in visiting <a href='InAction.md'>InAction</a> which discusses a case study of using Truesight for a real world task. Apologies to Opera users (and to myself as well) for distorted layout - all other mainstream browsers display this page finely and I'm not enough pro in web design to fix that.</td></tr>

<tr><td></td></tr>
<tr><td></td></tr>

<tr><td>
Q: Why not have separate nodes for Throw/Rethrow or for Catch/Fault/Filter or something else, just like CCI does?</td></tr>
<tr><td>A: Because then you gain too much granularity, i.e. lose the possibility to easily match all throws and lose the possibility to convert between catch/filter on the fly. The prior point can be addressed by introducing a complex inheritance hierarchy (as it's indeed done in CCI), which arguably raises perception complexity and doesn't solve the latter point. Generally, I just don't feel comfortable with this idea, so it got left out.<br>
</td></tr>

<tr><td>Q: In <a href='AstDesign.md'>AstDesign</a> you described how nice it is to have a read-write object model. However lambda's Sig and operator's OperatorType are readonly. Why's that?</td></tr>
<tr><td>A: These two places are the only ones where I prohibit modifications of certain properties of a node. That's because those modification might result in a change of children count which will lead to unpleasant and possible unexpected side-effects. Finally, my experience of using Truesight didn't involve a necessity for such changes so far and that certainly rings a bell to me. However, if you really need to do that, use <a href='http://code.google.com/p/truesight-lite/wiki/AstDesign'>traversers</a> to perform necessary morphing.</td></tr>

<tr><td></td></tr>
<tr><td></td></tr>

<tr><td>Q: Why do both BinaryOperator and UnaryOperator share the same NodeType?</td></tr>
<tr><td>A: Experience of developing Conflux in comparison with experience of working with System.Linq.Expressions proves that it's much more convenient to have a possibility of matching ALL operators and only then perform sub-matches on different OperatorTypes.</td></tr>

<tr><td></td></tr>
<tr><td></td></tr>

<tr><td>Q: Then why is Conditional a separate node rather than a subtype of Operator?</td></tr>
<tr><td>A: Ternary conditional operator is so much different from other operators that it needs different treatment. For example, when inferring types for the expression tree logic necessary to process conditionals and other operators differs drastically. Same for prettyprinting, same for codegeneration and so on.</td></tr>

<tr><td></td></tr>
<tr><td></td></tr>

<tr><td>Q: Why is Catch a subtype of Block, but Iter and Using are not?</td></tr>
<tr><td>A: Because latter nodes also feature an Init expression that is a logical parent of their body.</td></tr>

<tr><td></td></tr>
<tr><td></td></tr>

<tr><td>Q: Why introduce a special Clause node whose sole purpose is to be an empty base type for Catch/Finally?</td></tr>
<tr><td>A: That's an experiment based on the hypothesis that there will be a necessity of matching all clauses of the try. Since IL parser doesn't currently support protected regions, it's just theoretizing and ain't based on any practical evidence.</td></tr>

<tr><td></td></tr>
<tr><td></td></tr>

<tr><td>Q: Why do Clause and its subtypes have default implementation of Traverse while all other nodes crash as unsupported?</td></tr>
<tr><td>A: Because in most cases special treatment of clauses must be done by parent Try. If parent doesn't care about its clauses, they must be processed as normal Blocks.</td></tr>

<tr><td></td></tr>
<tr><td></td></tr>

<tr><td>Q: Why prohibit modifications of Locals and Params?</td></tr>
<tr><td>A: No special reason - that's just because certain piece of functionality isn't implemented yet. To improve performance Domain caches types of HIR nodes. Certainly, cache gets flushed whenever something gets changed. However, currently I don't track changes to locals' Type property, so I make it immutable for now.</td></tr>

<tr><td></td></tr>
<tr><td></td></tr>

<tr><td>Q: Why cache type inference results?</td></tr>
<tr><td>A: That's a part of an ongoing effort to marry AST and VS debugger.<br>
The next step would be caching results of DumpAsText or otherwise debugging performance is extremely slow.</td></tr>

<tr><td></td></tr>
<tr><td></td></tr>

<tr><td>Q: Then why strive for pleasant debugging experience if it's so much difficult to achieve?</td></tr>
<tr><td>A: I don't really know. The more I try, the less I like the result...<br>
Introducing comprehensive dumping made debugging very slow (please, don't tell me to turn off implicit evaluations!). Introducing caching for alleviating the problem described above made me develop the entire INPC infrastructure. Introducing debugger type proxies required shitloads of useless code to be written and, even worse, duplicated. Introducing debugger visualizers made type proxies useless since they don't work together. I think that apparently I'll kill all this stuff like I've done with RO/RW separation for AST classes with the use of interfaces.</td></tr>

</tbody></table>