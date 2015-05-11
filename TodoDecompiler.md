#1. Improve precision of decompiler (major points):
  * Work correctly with code generated in release mode.
  * Restore correct order of if/conditional clauses.
  * Support for conditional and coalescing operators.
  * Support try/catch/finallies in both IL parser and decompiler.
  * Support for switches.
  * Do not crash on irregular control flow but rather emit labels/gotos.
  * Decompile lambdas and their invocations.
  * Decompile generators.

#2. Review the entire algorithm in order to find potential fail points then create an exception hierarchy and implement decent error handling and reporting.

#3. Think about the way of tracking source IILOps, so that we can bind ourselves to real code. Potential problems:
  * Track those only for expressions and statements need just to gather the info from children.
  * What if we rewrite the AST by the means of manual rehashing?
  * What if we rewrite the AST by the means of visitor (i.e. partially using default VisitXXX code)?

#4. Improve precision of decompiler (minor points):
  * Support for array initialization literals, e.g. decompile "new int[.md](.md){1, 2, 3}" from
```
L_000a: ldtoken valuetype
<PrivateImplementationDetails>{64247C1A-43F1-4725-B2CA-C301EAFC3BCC}/__StaticArrayInitTypeSize=12
<PrivateImplementationDetails>{64247C1A-43F1-4725-B2CA-C301EAFC3BCC}::$$method0x600006e-1
L_000f: call void [mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(
class [mscorlib]System.Array, valuetype [mscorlib]System.RuntimeFieldHandle)
```
  * Support for collection and object initializers (and their mixes).
  * Support for pointers and related syntactic constructs (e.g. fixed, stackalloc and possible others).
  * There were other C#-specific thingies compiled into `<PrivateImplementationDetails>`, tho I don't remember those.
  * In general, try to exhaust all tricks of C# compiler, e.g. higher and lower bounds of arrays, the "unsafe" annotation for methods.
  * Also take a look at the CCI project - it has very nice level of elaboration.
  * Also compare price of adding new patterns to CCI and to our project (i.e. compare architectures).
  * Decompile arbitrary control flow (check [Cifuentes' thesis](http://www.itee.uq.edu.au/~cristina/dcc.html) for some ideas).

#5. Implement pipeline extensibility so that anyone can customize the pipeline with the use of a few attributes. That would require rethinking [XenoGears.Pipelines](http://code.google.com/p/xenogears/source/browse/trunk/XenoGears/Pipelines) so that it has acceptable performance and is developer-friendly.

#6. Improve prettyprinting:
  * Think how to verbatimly decompile tests of ifs (e.g. see ComplexConditions for the example of how not to).
  * Think about where it is appropriate to add brackets for easier reading (very long arguments of an operator).
  * Think about where it is appropriate to add newlines for easier reading (semantic blocks, very long expressions).
  * Omit unnecessary casts, e.g.: in the OmitUnnecessaryCasts test, casts to ptrs when working with unsigned array indices, boxing/unboxing.
  * Will it be better for readability to omit implicit casts (note: think how this will work with "var" declarations)?
  * Detect explicit interface implementation invocations.
  * Detect omitted "castclass" instructions used to upcast before a member invocation.
  * Automatically add @'s to variable names that coincide with keywords.
  * Do something if variable names cannot be expressed in C#.
  * Implement full C# regeneration functionality.
  * What to do with things that lack direct analogues in C#: non-virtual calls emitted for virtual methods, calling base ctors in the middle of child ctor, varargs in lambdas, calling static methods from an interface, using ldtoken to load non-type tokens, filter and fault blocks?