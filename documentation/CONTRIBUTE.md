# Debugging

The `Sample Debugging App` inside this repository serves to debug the profiler.
To get it going please follow these steps:
	* In Visual Studio, select the `Debug` solution config and `x64` as solution platform.
	* Build the profiler.
	* In the Sample Debugging App properties go to the `Debug` section and select `Open debug launch settings UI`.
	* Make sure that if a `Profiler.yml` configuration file exists in the DLLs folder (`.\Profiler\bin\Debug\`), it should enable profiling for `Sample_Debugging_App.exe`
	* Start the Sample Debugging App via Visual Studio. 
	
Your breakpoints in the profiler should now be working correctly and help you with debugging.