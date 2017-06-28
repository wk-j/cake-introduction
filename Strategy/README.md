```
/// Synchronize by checking all folders/files one-by-one.
/// This strategy is used if the CMIS server does not support the ChangeLog feature.
/// 
/// for all remote folders:
///     if exists locally:
///       recurse
///     else
///       if in database:
///         delete recursively from server // if BIDIRECTIONAL
///       else
///         download recursively
/// for all remote files:
///     if exists locally:
///       if remote is more recent than local:
///         download
///       else
///         upload                         // if BIDIRECTIONAL
///     else:
///       if in database:
///         delete from server             // if BIDIRECTIONAL
///       else
///         download
/// for all local files:
///   if not present remotely:
///     if in database:
///       delete
///     else:
///       upload                           // if BIDIRECTIONAL
///   else:
///     if has changed locally:
///       upload                           // if BIDIRECTIONAL
/// for all local folders:
///   if not present remotely:
///     if in database:
///       delete recursively from local
///     else:
///       upload recursively               // if BIDIRECTIONAL

/// True if all content has been successfully synchronized.
/// False if anything has failed or been skipped.
```