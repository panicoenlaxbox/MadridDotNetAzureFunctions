using System;
using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Azure.WebJobs.Hosting;
using CustomBindings;

[assembly: WebJobsStartup(typeof(CustomBindingsStartup))]
namespace CustomBindings
{
    public class CustomBindingsStartup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            builder.AddFileReaderBinding();
        }
    }
}

namespace CustomBindings
{
    [Binding]
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    public class FileReaderBindingAttribute : Attribute
    {
        [AutoResolve]
        // Automatically try to get a value from the configuration system
        // AutoResolve attribute will try and get the Location value from the local.settings.json file of the Azure Function that uses the binding
        public string Location { get; set; }
    }

    public class FileReaderModel
    {
        public string FullFilePath { get; set; }
        public string Content { get; set; }
    }

    [Extension("FileReaderBinding")]
    public class FileReaderBinding : IExtensionConfigProvider
    {
        // It adds a rule that binds the FileReaderModel to the input of the binding
        public void Initialize(ExtensionConfigContext context)
        {
            var rule = context.AddBindingRule<FileReaderBindingAttribute>();
            rule.BindToInput<FileReaderModel>(BuildItemFromAttribute);
        }

        // Performs the actual work
        // In this case, it tries to access the file in the location that is in the args and reads the text from that file and returns it.
        private FileReaderModel BuildItemFromAttribute(FileReaderBindingAttribute arg)
        {
            if (arg.Location.EndsWith("win.ini"))
            {
                throw new ArgumentException("win.ini is forbidden");
            }
            var content = string.Empty;
            if (File.Exists(arg.Location))
            {
                content = File.ReadAllText(arg.Location);
            }

            return new FileReaderModel
            {
                FullFilePath = arg.Location,
                Content = content
            };
        }
    }

    // Creates an extension method that we can use in IWebJobsStartup to initialize the binding
    public static class MyFileReaderBindingExtension
    {
        public static IWebJobsBuilder AddFileReaderBinding(this IWebJobsBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddExtension<FileReaderBinding>();
            return builder;
        }
    }
}
