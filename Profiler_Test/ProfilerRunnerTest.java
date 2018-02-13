/*-----------------------------------------------------------------------+
 | eu.cqse.conqat.engine.testgap
 |                                                                       |
   $Id: Package.java 11387 2014-01-31 14:12:40Z goeb $            
 |                                                                       |
 | Copyright (c)  2009-2013 CQSE GmbH                                 |
 +-----------------------------------------------------------------------*/
package eu.cqse.conqat.engine.testgap.profiler;

import static org.junit.Assert.assertTrue;

import java.io.File;
import java.io.IOException;
import java.util.Set;

import org.conqat.lib.commons.collections.CollectionUtils;
import org.conqat.lib.commons.io.ProcessUtils;
import org.conqat.lib.commons.io.ProcessUtils.ExecutionResult;
import org.junit.Test;

/**
 * Test case for coverage profiler. This will only execute and assert on Windows
 * systems.
 */
public class ProfilerRunnerTest extends ProfilerTestBase {

	/** The name of the directory in which the test programs are located. */
	private static final String TEST_PROGRAMS_DIR = "test-programs";

	/** The trace assembly keys of PDFizer's application assemblies. */
	private static final Set<String> PDFIZER_ASSEMBLIES = CollectionUtils.asHashSet("2", "3", "9");

	/**
	 * The trace assembly keys of the GeneratedTest's application assemblies.
	 */
	private static final Set<String> GEN_TEST_ASSEMBLIES = CollectionUtils.asHashSet("2", "3", "4");

	/** Test the PdfizerConsole application. */
	@Test
	public void testPdfizer() throws IOException {
		runTest("PdfizerConsole.exe", PDFIZER_ASSEMBLIES);
	}

	/** Test the generated application. */
	@Test
	public void testGenerated() throws IOException {
		runTest("GeneratedTest.exe", GEN_TEST_ASSEMBLIES);
	}

	/**
	 * Executes a test.
	 * 
	 * @param application
	 *            Path to the application to profile.
	 * @param assembliesToCompare
	 *            Assembly IDs of the relevant assemblies to use for test
	 *            assertion.
	 */
	public void runTest(String application, Set<String> assembliesToCompare) throws IOException {
		runProfiler(application);
		assertNormalizedTraceFileEqualsReference(new File(getAbsoluteExePath(application + "_reference.txt")),
				getTmpDirectory(), assembliesToCompare);
	}

	/** Executes the test application with the profiler attached. */
	private void runProfiler(String application) throws IOException {
		ProcessBuilder pb = new ProcessBuilder(getAbsoluteExePath(application));
		registerProfiler(pb, getTmpDirectory(), false, getBitness());
		pb.directory(useTestFile(TEST_PROGRAMS_DIR));
		ExecutionResult result = ProcessUtils.execute(pb);
		assertTrue("Program " + application + " did not execute properly.", result.getStdout().contains("SUCCESS"));
	}

	/**
	 * Returns the absolute path to the given file in the
	 * {@link #TEST_PROGRAMS_DIR}.
	 */
	private String getAbsoluteExePath(String filename) {
		return useTestFile(TEST_PROGRAMS_DIR + File.separator + filename).getAbsolutePath();
	}
}
