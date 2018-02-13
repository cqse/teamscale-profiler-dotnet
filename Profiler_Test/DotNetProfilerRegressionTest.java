/*-----------------------------------------------------------------------+
 | eu.cqse.conqat.engine.testgap
 |                                                                       |
   $Id$
 |                                                                       |
 | Copyright (c)  2009-2015 CQSE GmbH                                 |
 +-----------------------------------------------------------------------*/
package eu.cqse.conqat.engine.testgap.profiler;

import java.io.File;
import java.io.IOException;

import org.conqat.engine.core.bundle.BundleException;
import org.conqat.lib.commons.collections.CollectionUtils;
import org.conqat.lib.commons.filesystem.FileOnlyFilter;
import org.conqat.lib.commons.filesystem.FileSystemUtils;
import org.conqat.lib.commons.io.ProcessUtils;
import org.junit.Before;
import org.junit.Test;

/**
 * Regression test for the .NET coverage profiler. Since the profiler is written
 * in C++ and binds to Windows APIs, this test is a no-op on any OS other than
 * Windows.
 * <p>
 * Executes a test application with the profiler attached and compares the trace
 * file with a reference.
 * <p>
 * Location of the data used by this test:
 * <ul>
 * <li>The profiler binaries are located in the resources folder.
 * <li>The reference output is located in the test-data folder.
 * </ul>
 */
public class DotNetProfilerRegressionTest extends ProfilerTestBase {

	/**
	 * The name of the trace file that is created in the {@link #traceDirectory} of
	 * the analysis input data.
	 */
	private static final String TRACE_FILE_NAME = "trace.txt";

	/**
	 * The temporary directory that contains the input for the Test Gap dashboard.
	 */
	private File dashboardInputDirectory;

	/** The binary that is being profiled. */
	private File binaryFile;

	/** The path to which to copy the resulting trace file. */
	private File traceDirectory;

	/** The temporary directory into which all output is written. */
	private File workingDirectory;

	/**
	 * The location to which to save the trace file produced by the profiler
	 * (includes the name of the trace file).
	 */
	private File outputTraceFile;

	/** {@inheritDoc} */
	@Override
	@Before
	public void setUp() throws BundleException, IOException {
		super.setUp();

		File templateDirectory = useTestFile("dashboard-input");
		dashboardInputDirectory = new File(getTmpDirectory(), "input");
		workingDirectory = new File(getTmpDirectory(), "output");
		binaryFile = new File(dashboardInputDirectory, "v1/Binaries/ProfilerTestee.exe");
		traceDirectory = new File(dashboardInputDirectory, "v1/Traces");
		outputTraceFile = new File(traceDirectory, TRACE_FILE_NAME);
		new File(workingDirectory, "testgaps.csv");

		deleteTmpDirectory();
		FileSystemUtils.copyFiles(templateDirectory, dashboardInputDirectory, new FileOnlyFilter());
		FileSystemUtils.mkdirs(traceDirectory);
		FileSystemUtils.mkdirs(workingDirectory);
	}

	/**
	 * Tests the normal .NET 4 profiler by executing the test application in "all"
	 * mode.
	 */
	@Test
	public void testDotNet4FullAll() throws IOException {
		runTest("all", false);
	}

	/**
	 * Tests the normal .NET 4 profiler by executing the test application in "none"
	 * mode.
	 */
	@Test
	public void testDotNet4FullNone() throws IOException {
		runTest("none", false);
	}

	/**
	 * Tests the light .NET 4 profiler by executing the test application in "all"
	 * mode.
	 */
	@Test
	public void testDotNet4LightAll() throws IOException {
		runTest("all", true);
	}

	/**
	 * Tests the light .NET 4 profiler by executing the test application in "none"
	 * mode.
	 */
	@Test
	public void testDotNet40LightNone() throws IOException {
		runTest("none", true);
	}

	/**
	 * Executes a test for the given profiler with the given application mode.
	 */
	public void runTest(String applicationMode, boolean isLightMode) throws IOException {
		runProfiler(applicationMode, isLightMode);

		FileSystemUtils.copyFile(getTraceFile(workingDirectory), outputTraceFile);

		String profilerName = "profiler-dotnet-newer";
		if (isLightMode) {
			profilerName = "profiler-dotnet-newer-light";
		}

		File referenceTraceFile = useTestFile("reference-" + profilerName + "-" + applicationMode + ".txt");
		assertNormalizedTraceFileEqualsReference(referenceTraceFile, workingDirectory, CollectionUtils.asHashSet("2"));
	}

	/** Executes the test application with the profiler attached. */
	private void runProfiler(String applicationCommandLineArgument, boolean isLightMode) throws IOException {
		ProcessBuilder pb = new ProcessBuilder(binaryFile.getAbsolutePath(), applicationCommandLineArgument);

		registerProfiler(pb, workingDirectory, isLightMode, "32");

		pb.directory(workingDirectory);

		ProcessUtils.execute(pb);
	}
}
