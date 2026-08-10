Please prefix each entry with one of: 

- [breaking change]
- [feature]
- [fix]
- [documentation]

# Next Release

# v26.8.0
- [breaking change] Removed feature to upload raw .NET trace files
- [feature] The Teamscale username and access key can be provided via the `TEAMSCALE_USER` and `TEAMSCALE_ACCESSKEY` environment variables, which take precedence over the `username` and `accessKey` options in the config file
- [feature] Support for recording and uploading Testwise Coverage
- [fix] The `teamscale` section of the config file is now validated: previously an incomplete section was accepted and the upload failed later with an authentication error
- [fix] .NET Framework subprocesses were not traced when UploadDaemon was active
- [fix] Misspelled profiler option names in the config file were silently ignored, they are now reported as a warning in the trace file

# v25.12.0
- [fix] Removed feature to upload coverage to multiple Teamscale projects via embedded resources, as it unexpectedly locked DLL files

# v25.08.0
- [feature] Removed `--config-from-env` option from the Upload Daemon: now it defaults to the `COR_PROFILER_CONFIG` environment variable

# v24.5.2
- [fix] Assembly patterns could not match on full assembly name when using `@AssemblyDir` as PDB directory

# v24.5.1
- [fix] Upload Daemon uploaded to Artifactory in the wrong format

# v24.5.0
- [fix] Updated legacy API calls in Upload Daemon

# v24.2.0
- [fix] Log message when trace directory is not writable
- [feature] Added support to upload coverage to multiple Teamscale projects with different revisions.

# v23.6.0
- [feature] Support for uploading to artifactory with the Teamscale default artifact storage schema.
- [feature] Display error window and add Event Log error entry when the Profiler.yml could not be loaded
- [fix] Upload Daemon failed to upload when Teamscale URL was provided with trailing slash
- [fix] Default Profiler.yml configuration and Upload Daemon failed to load for .NET Core environment

# v22.8.0
- [feature] support for .NET core PDBs

# v22.7.0
- [feature] Upload Daemon sends distinct user agent header for Teamscale uploads
- [fix] Updated Newtonsoft.JSON to version 13.0.1


# v22.4.0
- [feature] Caching symbols for better performance when converting many trace files to line coverage.
- [fix] Upload Daemon could be started multiple times

# v19.8.0
- [fix] async upload bug
