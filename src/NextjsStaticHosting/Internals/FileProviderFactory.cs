using Microsoft.Extensions.FileProviders;

namespace NextjsStaticHosting.Internals
{
    internal class FileProviderFactory
    {
        public virtual IFileProvider CreateFileProvider(string physicalRoot) => new PhysicalFileProvider(physicalRoot);
    }
}
