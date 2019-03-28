[1mdiff --git a/Profiler/TraceLog.cpp b/Profiler/TraceLog.cpp[m
[1mindex 774175f..ad1418c 100644[m
[1m--- a/Profiler/TraceLog.cpp[m
[1m+++ b/Profiler/TraceLog.cpp[m
[36m@@ -15,14 +15,6 @@[m [mnamespace {[m
 	const char* LOG_KEY_JITTED = "Jitted";[m
 }[m
 [m
[31m-TraceLog::TraceLog()[m
[31m-{[m
[31m-}[m
[31m-[m
[31m-TraceLog::~TraceLog()[m
[31m-{[m
[31m-}[m
[31m-[m
 [m
 void TraceLog::writeJittedFunctionInfosToLog(std::vector<FunctionInfo>* functions)[m
 {[m
