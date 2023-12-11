/*
	Copyright © Bryan Apellanes 2015  
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Bam.Net.Data.Schema;
//using Bam.Net.Presentation.Handlebars;
using Bam.Net.ServiceProxy;
using Microsoft.CodeAnalysis;
using MongoDB.Driver;

namespace Bam.Net.Data.Repositories
{
    public partial class DtoModel
	{
		IRenderer _renderer;
		public DtoModel(Type dynamicDtoType, string nameSpace, IRenderer renderer = null)
		{
			TypeName = dynamicDtoType.Name;
			_renderer = renderer;
			List<string> properties = new System.Collections.Generic.List<string>();
			HashSet<Type> types = new HashSet<Type>();
			foreach(PropertyInfo p in dynamicDtoType.GetProperties())
			{
				Type type = (p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>)) ? Nullable.GetUnderlyingType(p.PropertyType) : p.PropertyType;
				properties.Add("\t\tpublic {0} {1} {{get; set;}}\r\n".Format(type.Name, p.Name));
				types.Add(type);
			}
			Properties = properties.ToArray();
			DtoType = dynamicDtoType;
			Namespace = nameSpace;
			MetadataReferenceResolver = new MetadataReferenceResolver(types.ToArray());
			ReferenceTypes = types;
			CleanTypeName();
		}

        public DtoModel(string nameSpace, string typeName, params DtoPropertyModel[] propertyModels)
        {
            List<string> properties = new List<string>();
            HashSet<Type> types = new HashSet<Type>();
            foreach(DtoPropertyModel p in propertyModels)
            {
                properties.Add("\t\tpublic {0} {1} {{get; set;}}\r\n".Format(p.PropertyType, p.PropertyName));
                types.Add(p.PropertyInfo.PropertyType);
            }
            MetadataReferenceResolver = new MetadataReferenceResolver(types.ToArray());
            ReferenceTypes = types;
            TypeName = typeName;
            Properties = properties.ToArray();
            Namespace = nameSpace;
            CleanTypeName();
        }

        public DtoModel(string nameSpace, string typeName, Dictionary<object, object> propertyValues)
        {
	        TypeName = typeName;
	        List<string> propertyNames = new List<string>();
	        HashSet<Type> types = new HashSet<Type>();
	        foreach (object key in propertyValues.Keys)
	        {
		        string propertyName = key.ToString();
		        object propertyValue = propertyValues[key] == null ? new object() : propertyValues[key];
		        Type type = propertyValue.GetType();
		        types.Add(type);
		        string propertyTypeName = type.Name;
		        propertyNames.Add($"\t\tpublic {propertyTypeName} {propertyName} {{get; set;}}\r\n");
	        }
	        MetadataReferenceResolver = new MetadataReferenceResolver(types.ToArray());
	        ReferenceTypes = types;
	        Properties = propertyNames.ToArray();
	        Namespace = nameSpace;
	        CleanTypeName();
        }

        protected HashSet<Type> ReferenceTypes { get; set; }
        public MetadataReferenceResolver MetadataReferenceResolver { get; private set; }

        public string Usings => GetUsings();
        public string TypeName { get; set; }
		public string Namespace { get; set; }
		public string[] Properties { get; set; }

		public Type DtoType { get; set; }
        public string Render()
        {
			return _renderer.Render(this);//Bam.Net.Handlebars.Render("Dto", this);
        }

        private string GetUsings()
		{
			StringBuilder result = new StringBuilder();
			ReferenceTypes.Each(t => result.Append($"\tusing {t.Namespace};\r\n"));
			return result.ToString();
		}

		private void CleanTypeName()
		{
			TypeName = CleanTypeName(TypeName);
		}

		internal static string CleanTypeName(string typeName)
		{
			return typeName.Replace(".", "_").Replace("/", "");
		}
	}
}
