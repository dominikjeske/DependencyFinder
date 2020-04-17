using Caliburn.Micro;
using DependencyFinder.Core;
using DependencyFinder.UI.ViewModels;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace DependencyFinder.UI
{
    //TODO http://blogs.microsoft.co.il/iblogger/2015/01/27/catching-exceptions-from-caliburn-micro-actionmessage-invocation/
    public class Bootstrapper : BootstrapperBase
    {
        private SimpleContainer container;

        public Bootstrapper()
        {
            Initialize();
        }

        protected override void Configure()
        {
            container = new SimpleContainer();

            BindingScope.AddChildResolver<Fluent.Ribbon>(FluentRibbonChildResolver);

            container.Singleton<IWindowManager, WindowManager>();
            container.Singleton<ISolutionManager, SolutionManager>();
            container.Singleton<ILogger, SimpleLogger>();
            container.PerRequest<ShellViewModel>();

            var configuration = GetConfiguration();
            var appSettings = new AppSettings();
            configuration.GetSection("AppSettings").Bind(appSettings);
            container.RegisterInstance(typeof(AppSettings), "", appSettings);
        }

        private static IEnumerable<DependencyObject> FluentRibbonChildResolver(Fluent.Ribbon ribbon)
        {
            foreach (var ti in ribbon.Tabs)
            {
                foreach (var group in ti.Groups)
                {
                    foreach (var obj in BindingScope.GetNamedElements(group))
                        yield return obj;
                }
            }
        }

        protected override void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.ToString());
            e.Handled = true;
        }

        protected override void OnExit(object sender, EventArgs e)
        {
            var solutionManager = container.GetInstance<ISolutionManager>();
            solutionManager.Dispose();

            base.OnExit(sender, e);
        }

        private static IConfigurationRoot GetConfiguration()
        {
            var builder = new ConfigurationBuilder()
                            .SetBasePath(Directory.GetCurrentDirectory())
                            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            return builder.Build();
        }

        protected override void OnStartup(object sender, StartupEventArgs e)
        {
            DisplayRootViewFor<ShellViewModel>();
        }

        protected override object GetInstance(Type service, string key)
        {
            return container.GetInstance(service, key);
        }

        protected override IEnumerable<object> GetAllInstances(Type service)
        {
            return container.GetAllInstances(service);
        }

        protected override void BuildUp(object instance)
        {
            container.BuildUp(instance);
        }
    }
}