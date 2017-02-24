using Cirrious.CrossCore.IoC;

namespace MWF.Mobile.Core
{

    public class App : Cirrious.MvvmCross.ViewModels.MvxApplication
    {

        public override void Initialize()
        {
            CreatableTypes()
                .EndingWith("Service")
                .AsInterfaces()
                .RegisterAsLazySingleton();

            CreatableTypes()
                .EndingWith("Repository")
                .AsInterfaces()
                .RegisterAsLazySingleton();

            CreatableTypes()
                .EndingWith("Repositories")
                .AsInterfaces()
                .RegisterAsLazySingleton();
				
            RegisterAppStart<ViewModels.StartupViewModel>();
        }

    }

}