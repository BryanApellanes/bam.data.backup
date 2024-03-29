/*
	Copyright © Bryan Apellanes 2015  
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bam.Net;
using System.Reflection;
using System.IO;
using System.CodeDom.Compiler;
using Bam.Net.Logging;
using Bam.Net.Configuration;

namespace Bam.Net.Data.Repositories
{
    /// <summary>
    /// A generator that will create Dto's from Dao's.
    /// Intended primarily to enable backup of
    /// Daos to an ObjectRepository
    /// </summary>
    public partial class DaoToDtoGenerator : Loggable, IAssemblyGenerator
    {
        public DaoToDtoGenerator() { }

        public DaoToDtoGenerator(Assembly daoAssembly)
        {
            this.DaoAssembly = daoAssembly;
        }

        public DaoToDtoGenerator(Dao daoInstance)
            : this(daoInstance.GetType().Assembly)
        { }

        public Assembly DaoAssembly
        {
            get;
            set;
        }

        [Verbosity(VerbosityLevel.Warning, SenderMessageFormat = "Unable to delete temp source directory: {TempDir}\r\n{ExceptionMessage}")]
        public event EventHandler DeleteTempSourceDirectoryFailed;

        /// <summary>
        /// Read by Loggable messages if deleting temp directory fails
        /// </summary>
        public string ExceptionMessage { get; set; }

        /// <summary>
        /// Read by Loggable messages if deleting temp directory fails
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
            Type oneDao = DaoAssembly.GetTypes().FirstOrDefault(t => t.HasCustomAttributeOfType<TableAttribute>());
            string writeSourceTo = Path.Combine(RuntimeSettings.ProcessDataFolder, "DtoTemp_{0}".Format(Dao.ConnectionName(oneDao)));
            DirectoryInfo sourceDir = SetSourceDir(writeSourceTo);

            WriteDtoSource(nameSpace, writeSourceTo);

            sourceDir.ToAssembly(fileName, out CompilerResults results);
            GeneratedAssemblyInfo result = new GeneratedAssemblyInfo(fileName, results);
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
            ); // this fluent stuff is setting the fileName to the Md5 hash of all the table names comma delimited
        }

        private string GetNamespace()
        {
            Args.ThrowIfNull(DaoAssembly, "DaoToDtoGenerator.DaoAssembly");

            Type oneTable = DaoAssembly.GetTypes().FirstOrDefault(t => t.HasCustomAttributeOfType<TableAttribute>());
            if (oneTable == null)
            {
                oneTable = DaoAssembly.GetTypes().FirstOrDefault();
                if (oneTable == null)
                {
                    Args.Throw<InvalidOperationException>("The specified DaoAssembly has no types defined");
                }
            }
            string nameSpace = oneTable.Namespace;
            return nameSpace;
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
                    throw ex;
                }
            }

            sourceDir.Create();
            return sourceDir;
        }
    }
}
