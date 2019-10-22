This project can be used for analyzing complex project that contains many solutions that share some dependencies.
Commands:

- List – allows list all solutions, projects and nugets in given root
- Nuget – allows to list nuget used by projects in given root
- Reference – allows to list all references of item in all solutions

All command have --help that describes all parameters we have to use.

Example:
- DependencyFinder.exe list “E:\Projects\Dependency\DependencyFinder\Test" -n –i
- DependencyFinder.exe nuget “E:\Projects\Dependency\DependencyFinder\Test" -i
- DependencyFinder.exe reference “E:\Projects\Dependency\DependencyFinder\Test" -p “E:\Projects\Dependency\DependencyFinder\Test\Common\CommonFull\CommonFull\CommonFull.csproj", -c “CommonFull.TestClass”
