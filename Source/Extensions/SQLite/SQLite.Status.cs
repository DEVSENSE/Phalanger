using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;

namespace PHP.Library.Data
{
    partial class SQLite
    {
        public enum Status
        {
            [ImplementsConstant("SQLITE_OK")]
            SQLITE_OK = 0,
            [ImplementsConstant("SQLITE_ERROR")]
            SQLITE_ERROR = 1,   /* SQL error or missing database */
            [ImplementsConstant("SQLITE_INTERNAL")]
            SQLITE_INTERNAL = 2,  /* Internal logic error in SQLite */
            [ImplementsConstant("SQLITE_PERM")]
            SQLITE_PERM = 3,  /* Access permission denied */
            [ImplementsConstant("SQLITE_ABORT")]
            SQLITE_ABORT = 4,  /* Callback routine requested an abort */
            [ImplementsConstant("SQLITE_BUSY")]
            SQLITE_BUSY = 5,  /* The database file is locked */
            [ImplementsConstant("SQLITE_LOCKED")]
            SQLITE_LOCKED = 6,  /* A table in the database is locked */
            [ImplementsConstant("SQLITE_NOMEM")]
            SQLITE_NOMEM = 7,  /* A malloc() failed */
            [ImplementsConstant("SQLITE_READONLY")]
            SQLITE_READONLY = 8,  /* Attempt to write a readonly database */
            [ImplementsConstant("SQLITE_INTERRUPT")]
            SQLITE_INTERRUPT = 9,  /* Operation terminated by sqlite3_interrupt()*/
            [ImplementsConstant("SQLITE_IOERR")]
            SQLITE_IOERR = 10,  /* Some kind of disk I/O error occurred */
            [ImplementsConstant("SQLITE_CORRUPT")]
            SQLITE_CORRUPT = 11,  /* The database disk image is malformed */
            [ImplementsConstant("SQLITE_NOTFOUND")]
            SQLITE_NOTFOUND = 12,  /* NOT USED. Table or record not found */
            [ImplementsConstant("SQLITE_FULL")]
            SQLITE_FULL = 13,  /* Insertion failed because database is full */
            [ImplementsConstant("SQLITE_CANTOPEN")]
            SQLITE_CANTOPEN = 14,  /* Unable to open the database file */
            [ImplementsConstant("SQLITE_PROTOCOL")]
            SQLITE_PROTOCOL = 15,  /* NOT USED. Database lock protocol error */
            [ImplementsConstant("SQLITE_EMPTY")]
            SQLITE_EMPTY = 16,  /* Database is empty */
            [ImplementsConstant("SQLITE_SCHEMA")]
            SQLITE_SCHEMA = 17,  /* The database schema changed */
            [ImplementsConstant("SQLITE_TOOBIG")]
            SQLITE_TOOBIG = 18,  /* String or BLOB exceeds size limit */
            [ImplementsConstant("SQLITE_CONSTRAINT")]
            SQLITE_CONSTRAINT = 19,  /* Abort due to constraint violation */
            [ImplementsConstant("SQLITE_MISMATCH")]
            SQLITE_MISMATCH = 20,  /* Data type mismatch */
            [ImplementsConstant("SQLITE_MISUSE")]
            SQLITE_MISUSE = 21,  /* Library used incorrectly */
            [ImplementsConstant("SQLITE_NOLFS")]
            SQLITE_NOLFS = 22,  /* Uses OS features not supported on host */
            [ImplementsConstant("SQLITE_AUTH")]
            SQLITE_AUTH = 23,  /* Authorization denied */
            [ImplementsConstant("SQLITE_FORMAT")]
            SQLITE_FORMAT = 24,  /* Auxiliary database format error */
            [ImplementsConstant("SQLITE_RANGE")]
            SQLITE_RANGE = 25,  /* 2nd parameter to sqlite3_bind out of range */
            [ImplementsConstant("SQLITE_NOTADB")]
            SQLITE_NOTADB = 26,  /* File opened that is not a database file */
            [ImplementsConstant("SQLITE_ROW")]
            SQLITE_ROW = 100,  /* sqlite3_step() has another row ready */
            [ImplementsConstant("SQLITE_DONE")]
            SQLITE_DONE = 101,  /* sqlite3_step() has finished executing */
        }
    }
}
