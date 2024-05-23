Please prefix each entry with one of: 

- [breaking change]
- [feature]
- [fix]
- [documentation]


# Next Release

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
