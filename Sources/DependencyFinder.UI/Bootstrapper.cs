using System;
using System.Collections.Generic;
using System.Windows;
using Caliburn.Micro;
using DependencyFinder.UI.ViewModels;

namespace DependencyFinder.UI
{
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
            

            container.PerRequest<ShellViewModel>();
        }

        static IEnumerable<DependencyObject> FluentRibbonChildResolver(Fluent.Ribbon ribbon)
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
