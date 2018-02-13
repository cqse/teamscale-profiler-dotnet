/*-----------------------------------------------------------------------+
 | eu.cqse.conqat.engine.testgap
 |                                                                       |
   $Id$
 |                                                                       |
 | Copyright (c)  2009-2015 CQSE GmbH                                 |
 +-----------------------------------------------------------------------*/
package eu.cqse.conqat.engine.testgap.profiler;

import static eu.cqse.conqat.engine.testgap.scope.traces.dotnet.DotNetTraceUtils.KEY_SEPARATOR;
import static eu.cqse.conqat.engine.testgap.scope.traces.dotnet.DotNetTraceUtils.LABEL_INLINED;
import static eu.cqse.conqat.engine.testgap.scope.traces.dotnet.DotNetTraceUtils.LABEL_JITTED;
import static eu.cqse.conqat.engine.testgap.scope.traces.dotnet.DotNetTraceUtils.keyValuesMapFor;
import static org.junit.Assert.assertEquals;
import static org.junit.Assume.assumeTrue;

import java.io.File;
import java.io.IOException;
import java.util.ArrayList;
import java.util.List;
import java.util.Set;

import org.conqat.engine.core.bundle.BundleException;
import org.conqat.engine.core.logging.testutils.LoggerMock;
import org.conqat.engine.dotnet.ila.AssemblyExecutorTestBase;
import org.conqat.lib.commons.assertion.CCSMAssert;
import org.conqat.lib.commons.collections.CollectionUtils;
import org.conqat.lib.commons.collections.ListMap;
import org.conqat.lib.commons.filesystem.FileSystemUtils;
import org.conqat.lib.commons.io.ProcessUtils;
import org.conqat.lib.commons.io.ProcessUtils.ExecutionResult;
import org.conqat.lib.commons.string.StringUtils;
import org.conqat.lib.commons.system.SystemUtils;

import eu.cqse.conqat.engine.testgap.BundleContext;

/**
 * Base class for testing the .NET Profiler.
 */
public abstract class ProfilerTestBase extends AssemblyExecutorTestBase {

	/** Environment variable name to enable the profiler. */
	private static final String PROFILER_ENABLE_KEY = "COR_ENABLE_PROFILING";

	/** Environment variable name for the profiler's class ID. */
	private static final String PROFILER_CLASS_ID_KEY = "COR_PROFILER";

	/** Environment variable name for the directory to store profiler traces. */
	private static final String PROFILER_TARGETDIR_KEY = "COR_PROFILER_TARGETDIR";

	/** Environment variable name for the path to the profiler DLL. */
	private static final String PROFILER_PATH_KEY = "COR_PROFILER_PATH";

	/** Environment variable name to enable the profiler's light mode. */
	private static final String PROFILER_LIGHT_MODE_KEY = "COR_PROFILER_LIGHT_MODE";

	/** The profiler's class ID. */
	private static final String PROFILER_CLASS_ID = "{DD0A1BB6-11CE-11DD-8EE8-3F9E55D89593}";

	/** {@inheritDoc} */
	@Override
	public void initBundleContext() throws BundleException {
		new BundleContext(createBundleInfo(BundleContext.class));
	}

	/** Makes sure tests run only on Windows with .NET 4 and newer. */
	@Override
	public void setUp() throws BundleException, IOException {
		assumeTrue("Profiler only runs on Windows", SystemUtils.isWindows());
		super.setUp();
		boolean isDotNet4 = executeProgramInResourceFolder("netversioninfo.bat").getStdout()
				.contains("ProductVersion:   4.");
		assumeTrue("Profiler only runs on .NET 4 and newer", isDotNet4);
	}

	/** Sets all environment variables the profiler needs. */
	protected void registerProfiler(ProcessBuilder pb, File targetDir, boolean useLightMode, String bitness) {

		String profilerDll = "profiler-dotnet-newer/Profiler" + bitness + ".dll";

		// set environment variables for the profiler
		pb.environment().put(PROFILER_PATH_KEY, getAbsoluteResourcePath(profilerDll));
		pb.environment().put(PROFILER_TARGETDIR_KEY, targetDir.getAbsolutePath());
		pb.environment().put(PROFILER_CLASS_ID_KEY, PROFILER_CLASS_ID);
		pb.environment().put(PROFILER_ENABLE_KEY, "1");
		if (useLightMode) {
			pb.environment().put(PROFILER_LIGHT_MODE_KEY, "1");
		}
	}

	/**
	 * Asserts that the trace file written by the profiler has the same contents as
	 * the given reference trace, modulo some normalization.
	 */
	protected void assertNormalizedTraceFileEqualsReference(File referenceTraceFile, File directory,
			Set<String> assembliesToCompare) throws IOException {
		assertEquals("The normalized contents of the trace files did not match",
				getNormalizedTraceContent(referenceTraceFile, assembliesToCompare),
				getNormalizedTraceContent(getTraceFile(directory), assembliesToCompare));
	}

	/**
	 * Returns the trace.
	 *
	 * @throws AssertionError
	 *             if not exactly one trace was written.
	 */
	protected File getTraceFile(File directory) {
		File[] traceFiles = directory.listFiles((dir, name) -> name.startsWith("coverage_"));

		CCSMAssert.isTrue(traceFiles.length > 0, "No coverage trace was written.");
		CCSMAssert.isTrue(traceFiles.length <= 1,
				"More than one coverage trace was written: " + StringUtils.concat(traceFiles, ", "));

		return traceFiles[0];
	}

	/**
	 * Executes a program from the resource folder.
	 */
	protected static ExecutionResult executeProgramInResourceFolder(String program) throws IOException {
		return ProcessUtils.execute(new String[] { getAbsoluteResourcePath(program) });
	}

	/** Gets the absolute path to a resource file. */
	private static String getAbsoluteResourcePath(String path) {
		return BundleContext.getInstance().getResourceManager().getAbsoluteResourcePath(path);
	}

	/**
	 * Returns the inlined and jitted methods for the testee assembly. All other
	 * trace file content may vary across machines or versions of the .NET
	 * framework, including the number of actually jitted methods (in mscorlib).
	 */
	private static String getNormalizedTraceContent(File traceFile, Set<String> assembliesToCompare)
			throws IOException {
		String content = FileSystemUtils.readFileUTF8(traceFile);

		ListMap<String, String> traceMap = keyValuesMapFor(content, new LoggerMock(), traceFile.getName());

		List<String> inlined = filterMethodInvocationsByAssemblyNumber(traceMap.getCollection(LABEL_INLINED),
				assembliesToCompare);
		List<String> jitted = filterMethodInvocationsByAssemblyNumber(traceMap.getCollection(LABEL_JITTED),
				assembliesToCompare);

		return StringUtils.concat(CollectionUtils.sort(CollectionUtils.unionSet(inlined, jitted)),
				StringUtils.LINE_SEPARATOR);
	}

	/**
	 * Filters invoked methods, keeping only those that contain allowed assembly
	 * keys. Each line in the output corresponds to one method invocation.
	 */
	private static List<String> filterMethodInvocationsByAssemblyNumber(List<String> methodInvocations,
			Set<String> assembliesToCompare) {

		List<String> methodIds = new ArrayList<>();

		for (String methodInvocation : methodInvocations) {
			String assemblyToken = StringUtils.getFirstParts(methodInvocation, 1, KEY_SEPARATOR);

			if (assembliesToCompare.contains(assemblyToken)) {
				methodIds.add(methodInvocation);
			}
		}
		return methodIds;
	}

	/**
	 * Returns "64" if running on an x64 OS and having a .NET framework x64
	 * installed, or "32" in case of an x86 .NET framework.
	 */
	protected static String getBitness() throws IOException {
		return executeProgramInResourceFolder("bitness-checker.exe").getStdout().substring(0, 2);
	}
}
