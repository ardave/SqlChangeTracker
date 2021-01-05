# SqlChangeTracker
In-Memory mirroring for SQL Server DB Table with Change Tracking Enabled

Best thought of as a tool to which you can periodically feed the output of

```
select
    yt.*,
    ct.*
from
    YourTable yt
    left join CHANGETABLE(CHANGES dbo.YourTable, 0) as ct on ct.Id = yt.Id
```

and the tool will maintain an in-memory representation of the most-recently retrieved database table contents,
with changes applied, and handling the SQL Server-related nuances of change reporting, to include absent
(cleaned up, etc.) changes.

** Be advised:  the ChangeTracker record type uses mutable internal state
