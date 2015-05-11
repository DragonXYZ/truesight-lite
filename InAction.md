Hi there! This page features an example of practical usage of Truesight in my GPGPU research project. It's mostly practical, so if you're here for some philosophy behind the design or for a reference of available functionality, please, check out the following wiki pages: [HirAndIdioms](HirAndIdioms.md) that describes supported semantics and how it gets mapped onto Truesight's idioms, [HirDesign](HirDesign.md) that tells about domain-specific concerns of designing a HIR (high-level intermediate representation _of code_) and [AstDesign](AstDesign.md) that discusses general philosophic questions of implementing AST data structures and infrastructure.

The task in question is transforming kernels written in the form that is traditional for CUDA into the form that is more or less efficiently executable on a multi-core CPU. The transformation algorithm closely follows the research work of Illinois University scientists ["MCUDA: An Efficient Implementation of CUDA Kernels on Multi-cores"](http://impact.crhc.illinois.edu/ftp/report/impact-08-01-mcuda.pdf). If you've got no idea about CUDA - fear not: basic ideas are very simple and we'll get to them in a moment.

```
void MatMulKernel(float[,] a, float[,] b, float[,] c)
{
    var row = this.BlockIdx.Y * this.BlockDim.Y + this.ThreadIdx.Y;
    var col = this.BlockIdx.X * this.BlockDim.X + this.ThreadIdx.X;
    if ((a.Height<float>() > row) && (b.Width<float>() > col))
    {
        var c_value = 0;
        for (var dim = 0; dim < a.Width<float>(); ++dim)
        {
            c_value += a[row, dim] * b[dim, col];
        }
        c[row, col] = c_value;
    }
}
```

Code sample below features the hello world of CUDA - the matrix multiplication sample. Some comments are in order:
  * Kernel doesn't have a return result - it passes the result back by mutating one or more of its parameters.
  * The algorithm features a single iteration of an implied loop. If you take a close look at the code above, you've notice that it computes only a single cell of the resulting matrix.
  * Inside the kernel you can use several built-in variables (that are emulated here by instance properties of the surrounding class) to find out indexes of current loop iteration. Generally speaking, index space is split into `GridDim` blocks that consist of `BlockDim` threads. Current block has the `BlockIdx` index within the grid and current thread has the `ThreadIdx` index within the block. If you're now familiar with the GPGPU buzz, you might wonder why not just have a flat grid. You'll see that later.
  * Except these three peculiarities, everything seems to look normally.

While this shape of the algorithm perfectly maps onto native architecture of GPUs, it must be preprocessed to be executable on a CPU. First and the most obvious preprocessing is to surround the kernel in the implied loop we've talked above. However, as it was mentioned, the loop has two levels, so we'll implement it as follows: 1) the kernel will executed the entire thread block, 2) the scheduler will distribute blocks of the grid across worker threads. So we need to get the following result:

```
void MatMulKernel(int3 blockIdx, float[,] a, float[,] b, float[,] c)
{
    for (var tid_z = 0; tid_z < x_this.BlockDim.Z; ++tid_z)
    {
        for (var tid_y = 0; tid_y < x_this.BlockDim.Y; ++tid_y)
        {
            for (var tid_x = 0; tid_x < x_this.BlockDim.X; ++tid_x)
            {
                var row = blockIdx.Y * x_this.BlockDim.Y + tid_y;
                var col = blockIdx.X * x_this.BlockDim.X + tid_x;

                // actual matrix multiplication - the code is the same as above
            }
        }
    }
}
```

This short intro just scratches the topic of crosscompiling CUDA kernels for execution on the CPU, since there still remains a number of details that need to be taken care of. For example: 1) variables used in the algorithm can be "shared", i.e. shared between all threads within a block (shared memory is much faster than global memory due to [hardware implementation details](http://xeno-by.livejournal.com/21841.html), so that's why it makes sense to have two levels of grid hierarchy), 2) algorithm might feature calls to `SyncThreads` that is, basically, a barrier for all threads within a block. This all means that we have not just to copy/paste the code and wrap it in a loop, but to significantly reshape it to preserve all the semantics.

Most of these details are taken into account in my quick and dirty implementation of MCUDA for .NET that's a part of the Truesight.Playground project. You can find corresponding code at http://code.google.com/p/truesight-lite/source/browse/#hg/Truesight.Playground/InAction. For now this code can be considered an idiomatic example of using Truesight - it illustrates most of the concepts discussed in [HirAndIdioms](HirAndIdioms.md) in much greater detail. Since it's a part of unit tests suite, it can also be used as the most up to date reference of the Truesight API - wiki might be stale, but unit tests certainly would not.