using System.IO;

namespace Flame.Compiler.Pipeline
{
    /// <summary>
    /// An assembly generated by a back-end.
    /// </summary>
    public interface ITargetAssembly
    {
        /// <summary>
        /// Writes this target assembly to a stream.
        /// </summary>
        /// <param name="output">
        /// An output stream.
        /// </param>
        void WriteTo(Stream output);
    }
}