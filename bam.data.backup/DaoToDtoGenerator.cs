/*
	Copyright © Bryan Apellanes 2015  
*/

using System.Reflection;
using Bam.Logging;

namespace Bam.Data.Repositories
{
    /// <summary>
    /// A generator that creates data transfer objects from data access objects.
    /// </summary>
    public class DaoToDtoGenerator : Loggable, IAssemblyGenerator
    {
        public DaoToDtoGenerator() { }

        public DaoToDtoGenerator(Assembly daoAssembly)
        {
            this.DaoAssembly = daoAssembly;
        }

        public Assembly DaoAssembly
        {
            get;
            set;
        }

        [Verbosity(VerbosityLevel.Warning, SenderMessageFormat = "Unable to delete temp source directory: {TempDir}\r\n{ExceptionMessage}")]
        public event EventHandler DeleteTempSourceDirectoryFailed;

        /// <summary>
        /// Gets or sets the message value that is read by Loggable messages if deleting temp directory fails.
        /// </summary>
        public string ExceptionMessage { get; set; }

        /// <summary>
        /// Gets or sets the 'TempDir' value that is read by Loggable messages if deleting temp directory fails.
        /// </summary>
        public string TempDir { get; set; }

        public void WriteSource(string writeSourceTo)
        {
            WriteDtoSource(GetNamespace(), writeSourceTo);
        }

        object _generateLock = new object();
        /// <summary>
        /// Implements IAssemblyGenerator.GenerateAssembly by delegating
        /// to GenerateDtoAssembly
        /// </summary>
        /// <returns></returns>
        public GeneratedAssemblyInfo GenerateAssembly()
        {
            lock (_generateLock)
            {
                return GenerateDtoAssembly();
            }
        }

        /// <summary>
        /// Generates a Dto assembly
        /// </summary>
        /// <returns></returns>
		public GeneratedAssemblyInfo GenerateDtoAssembly()
        {
            string nameSpace = GetNamespace();
            return GenerateDtoAssembly("{0}.Dtos".Format(nameSpace));
        }

        public GeneratedAssemblyInfo GenerateDtoAssembly(string nameSpace)
        {
            return GenerateDtoAssembly(nameSpace, GetDefaultFileName());
        }

        public GeneratedAssemblyInfo GenerateDtoAssembly(string nameSpace, string fileName)
        {
            Type? oneDao = DaoAssembly.GetTypes().FirstOrDefault(t => t.HasCustomAttributeOfType<TableAttribute>());
            string writeSourceTo = Path.Combine(RuntimeSettings.ProcessDataFolder, "DtoTemp_{0}".Format(Dao.ConnectionName(oneDao)));
            DirectoryInfo sourceDir = SetSourceDir(writeSourceTo);

            WriteDtoSource(nameSpace, writeSourceTo);

            sourceDir.ToAssembly(fileName, out byte[] bytes);
            GeneratedAssemblyInfo result = new GeneratedAssemblyInfo(fileName, bytes);
            result.Save();
            return result;
        }

        /// <summary>
        /// Write dto source code to the specified directory
        /// </summary>
        /// <param name="dir"></param>
        public void WriteDtoSource(DirectoryInfo dir)
        {
            WriteDtoSource(dir.FullName);
        }

        /// <summary>
        /// Write dto source code into the specified namespace placing generated files into the specified directory
        /// </summary>
        /// <param name="nameSpace"></param>
        /// <param name="writeSourceTo"></param>
        public void WriteDtoSource(string nameSpace, string writeSourceTo)
        {
            Args.ThrowIfNull(DaoAssembly, "DaoAssembly");

            foreach (Type daoType in DaoAssembly.GetTypes()
                .Where(t => t.HasCustomAttributeOfType<TableAttribute>()))
            {
                Dto.WriteRenderedDto(nameSpace, writeSourceTo, daoType, pi => pi.HasCustomAttributeOfType<ColumnAttribute>());
            }
        }

        /// <summary>
        /// Write dto source code to the specified directory
        /// </summary>
        /// <param name="writeSourceTo"></param>
        public void WriteDtoSource(string writeSourceTo)
        {
            WriteDtoSource($"{GetNamespace()}.Dtos", writeSourceTo);
        }

        public string GetDefaultFileName()
        {
            return "_{0}_{1}_.dll".Format(
                GetNamespace(),
                DaoAssembly.GetTypes()
                .Where(t => t.HasCustomAttributeOfType<TableAttribute>())
                .ToInfoHash()
            ); // this fluent stuff is setting the fileName to the SHA256 hash of all the table names comma delimited
        }

        private string GetNamespace()
        {
            Args.ThrowIfNull(DaoAssembly, "DaoToDtoGenerator.DaoAssembly");

            Type? oneTable = DaoAssembly.GetTypes().FirstOrDefault(t => t.HasCustomAttributeOfType<TableAttribute>());
            if (oneTable == null)
            {
                oneTable = DaoAssembly.GetTypes().FirstOrDefault();
                if (oneTable == null)
                {
                    Args.Throw<InvalidOperationException>("The specified DaoAssembly has no types defined");
                }
            }
            string? nameSpace = oneTable?.Namespace;
            return nameSpace ?? "DEFAULT_NAMESPACE";
        }

        private DirectoryInfo SetSourceDir(string writeSourceTo)
        {
            DirectoryInfo sourceDir = new DirectoryInfo(writeSourceTo);
            if (sourceDir.Exists)
            {
                try
                {
                    sourceDir.Delete(true);
                }
                catch (Exception ex)
                {
                    TempDir = sourceDir.FullName;
                    ExceptionMessage = Args.GetMessageAndStackTrace(ex);
                    FireEvent(DeleteTempSourceDirectoryFailed, EventArgs.Empty);
                    throw;
                }
            }

            sourceDir.Create();
            return sourceDir;
        }
    }
}
