# Debugging

The `Sample Debugging App` inside this repository serves to debug the profiler.
To get it going please follow these steps:
	* In Visual Studio, select the `Debug` solution config and `x64` as solution platform.
	* Build the profiler.
	* In the Sample Debugging App properties go to the `Debug` section and select `Open debug launch settings UI`.
	* Change `CORECLR_PROFILER_PATH_64` to the path of your `Profiler64.dll`. This path needs to be absolute and should look similar to `C:\\Dev\\teamscale-profiler-dotnet\\Profiler\\bin\\Debug\\Profiler64.dll`.
	* Start the Sample Debugging App via Visual Studio. 
	
Your breakpoints in the profiler should now be working correctly and help you with debugging.