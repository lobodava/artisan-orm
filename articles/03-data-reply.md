# The Artisan Way of Data Reply

> **Note (2024).** Originally published on [CodeProject in April 2017](https://www.codeproject.com/Articles/1181182/Artisan-Way-of-Data-Reply) and last revised June 2021. CodeProject has since gone read-only; this re-publication preserves the original content. A handful of small fixes were applied during conversion:
> - "your should never do this" → "you should never do this";
> - "the data respond" → "the data response";
> - "AngularJs" → "AngularJS";
> - C# samples that use plain primitives switched from `Int32` / `String` to `int` / `string` for readability. The class declarations below retain the historical, abbreviated presentation; use the library's current types rather than copying these declarations into an application.
>
> The web-client section (Step 6) uses **AngularJS** in the original; that framework reached end-of-life in 2022, but the *pattern* — single client-side service, allow-list of expected statuses, central error sink — translates one-to-one to modern fetch / axios / RxJS / TanStack Query. A footnote at the bottom of Step 6 sketches the modern equivalent.
>
> The DataReply system is part of Artisan.Orm — see [`Artisan.Orm/DataReply.cs`](../Artisan.Orm/DataReply.cs), [`DataReplyException.cs`](../Artisan.Orm/DataReplyException.cs), and the [Artisan Way of DataReply](https://github.com/lobodava/artisan-orm/wiki/Artisan-Way-of-Data-Reply) wiki page.
>
> — *Vadim Loboda, originally Apr 2017, revised Jun 2021*

> **Compatibility with the upcoming 4.1.0 release.** The original design discussion and publication history are preserved. The repository, service, JSON, and modern web-client examples below include explicitly identified current alternatives. These describe the source being prepared for 4.1.0, not a claim that the release is already published; see [4.1.0 changes](../CHANGELOG.md). DataReply is optional: use its SQL status/message protocol when business validation in a procedure needs structured details. Ordinary repository methods can return data directly, and an application can use its own result or HTTP error model.

A way to deliver more details about an error or exception from an SQL Server database through an ASP.NET web application to a web client.

This article is a continuation and development of the Artisan.Orm series:

- [How to Save an Object Graph in Master-Detail Relationship with One Stored Procedure](01-object-graph-saving.md)
- [Artisan.Orm — How to Reinvent the Wheel](02-artisan-orm.md)

The next article:

- [Combination of Id-ParentId and HierarchyId Approaches to Hierarchical Data](04-hierarchyid-combination.md)

The pattern described here can also be used completely independently.

## Introduction

Suppose we have a web application with a three-tier structure:

- a Web SPA client, which requests data from a web server with Ajax;
- a Web server / ASP.NET application;
- an SQL Server database.

Once a web client requests to get or save data, the data reply travels back from the database to UI through this pipeline.

On this way, all sorts of surprises can happen. An error may rise in the database on SQL Server, or an exception may occur in the ASP.NET application. The final recipient — a web client — should somehow be informed about what had happened: was the request executed correctly, or did something go wrong?

This article is an attempt to make a convenient and universal format of data reply that provides a way to pass the details of an exceptional case through the above pipeline from a database to a web client.

## Background

Some thoughts and propositions before we start.

### Two Categories of Exceptions

Errors and exceptions can be divided into two categories: **expected** and **unexpected**.

**Unexpected** exceptions are the result of a bug in code or an equipment failure — things we hope never happen in a perfect world. The usual reaction to such exceptions: inform the user and the admin about the fatal application error.

**Expected** exceptions are the result of inconsistent data saving, untimely requests, or other activities where a user is able to solve the issue by themselves. Examples:

- **Data validation** on the web server or in the database — such as uniqueness of a user's login or email.
- **Data concurrency** — when two users edit the same record in the database at the same time.
- **Data missing** — when the first user deletes a record before the second one saves it.
- **Data access denial** — when a data reply depends on a user-access level calculated in the database.

### Status Delivery via Http

The following paragraphs record the original motivation for the envelope; the current guidance after them corrects the distinction between HTTP and business outcomes.

How can a web client distinguish what kind of exception occurred? It is necessary to pass the status to it.

How to deliver this status from a web server to a web client? The first idea that comes to mind is to use HTTP status codes…

…and this idea turns out to be worthless. It is like asking the airplane captain to inform your mom that you are in trouble via the official radio frequency used for landing and taking off — although you just forgot your lunch box on the kitchen table.

HTTP status codes are used to inform recipients about a transmission status. Browsers use that status to react. Mixing a transmission status and a data-reply status sooner or later leads us to the problem of unexpected browser reactions, or to the impossibility of finding an appropriate code for our needs.

The better idea, I think, is to make a universal format of data reply — something like a wrapping object where `Data` and `Status` are the properties.

> **Current guidance.** HTTP is an application-level protocol, not merely a transmission-status channel. An appropriate HTTP status and a structured response body complement each other. DataReply does not require returning HTTP 200 for failures or replacing the application's HTTP error conventions. Map domain outcomes at the web boundary; the controller and fetch examples below demonstrate one such policy. See [HTTP semantics](https://www.rfc-editor.org/rfc/rfc9110.html#section-15).

## Seven Steps

The task of delivering exception details through the data-reply pipeline can be divided into several steps:

1. **In the database**, find an exceptional case and output the necessary data.
2. **In a repository**, recognise the exceptional case and read the data about it.
3. **In a repository**, throw an exception so that a data service can handle it in C# good-practice way.
4. **In a data service**, get normal data or catch an exception, and create a universal data reply.
5. **In an ASP.NET Web API controller**, serialise the data reply into JSON format.
6. **In web-client data services**, get the JSON data, define the status of the data reply, take appropriate actions.
7. **In SPA controllers**, get a data reply, define its status, take appropriate actions.

## Universal Format of DataReply

### DataReply in JSON

The desirable data wrapper, after the ASP.NET Web API controller serialises it to JSON, should look like:

```json
{
  "status": "ok",
  "data": { },
  "messages": [ ]
}
```

### DataReply in C#

A C# object for serialisation must have the same public properties. After a series of experiments, I found the optimal — at least for me — structure of the `DataReply` class in C#.

The `DataReply` base class has only two properties: `Status` and `Messages`. The derived `DataReply<TData>` adds the `Data` property.

The following declarations explain the historical structure, rather than reproduce every current member or nullable annotation. `DataReplyMessage` uses public **fields**, which matters when configuring JSON serialization. Use the [package types](../Artisan.Orm/DataReply.cs) in application code; the explicit System.Text.Json configuration in Step 5 supplies the JSON contract shown here.

#### `DataReplyStatus`

```csharp
public enum DataReplyStatus
{
    Ok          ,
    Fail        ,
    Missing     ,
    Validation  ,
    Concurrency ,
    Denial      ,
    Error
}
```

#### `DataReplyMessage`

```csharp
[DataContract]
public class DataReplyMessage
{
    [DataMember]
    public string Code;

    [DataMember(EmitDefaultValue = false)]
    public string Text;

    [DataMember(EmitDefaultValue = false)]
    public long? Id;

    [DataMember(EmitDefaultValue = false)]
    public object Value;
}
```

#### `DataReply`

```csharp
[DataContract]
public class DataReply
{
    [DataMember]
    public DataReplyStatus Status { get; set; }

    [DataMember(EmitDefaultValue = false)]
    public DataReplyMessage[] Messages { get; set; }

    public DataReply()
    {
        Status   = DataReplyStatus.Ok;
        Messages = null;
    }

    public DataReply(DataReplyStatus status)
    {
        Status   = status;
        Messages = null;
    }

    public DataReply(DataReplyStatus status, string code, string text)
    {
        Status   = status;
        Messages = new[] { new DataReplyMessage { Code = code, Text = text } };
    }

    public DataReply(DataReplyStatus status, DataReplyMessage message)
    {
        Status = status;
        if (message != null)
            Messages = new[] { message };
    }

    public DataReply(DataReplyStatus status, DataReplyMessage[] messages)
    {
        Status = status;
        if (messages?.Length > 0)
            Messages = messages;
    }

    public DataReply(string message)
    {
        Status   = DataReplyStatus.Ok;
        Messages = new[] { new DataReplyMessage { Text = message } };
    }

    public static DataReplyStatus? ParseStatus(string statusCode)
    {
        if (string.IsNullOrWhiteSpace(statusCode))
            return null;

        if (Enum.TryParse(statusCode, true, out DataReplyStatus status))
            return status;

        throw new InvalidCastException(
            $"Cannot cast string '{statusCode}' to DataReplyStatus enum. " +
            $"Available values: {string.Join(", ", Enum.GetNames(typeof(DataReplyStatus)))}");
    }
}
```

#### `DataReply<TData>`

```csharp
[DataContract]
public class DataReply<TData> : DataReply
{
    [DataMember(EmitDefaultValue = false)]
    public TData Data { get; set; }

    public DataReply(TData data) { Data = data; }
    public DataReply()           { Data = default; }

    public DataReply(DataReplyStatus status, string code, string text, TData data)
        : base(status, code, text)
    {
        Data = data;
    }

    public DataReply(DataReplyStatus status, TData data) : base(status)
    {
        Data = data;
    }

    public DataReply(DataReplyStatus status) : base(status)
    {
        Data = default;
    }

    public DataReply(DataReplyStatus status, string code, string text)
        : base(status, code, text)
    {
        Data = default;
    }

    public DataReply(DataReplyStatus status, DataReplyMessage replyMessage)
        : base(status, replyMessage)
    {
        Data = default;
    }

    public DataReply(DataReplyStatus status, DataReplyMessage[] replyMessages)
        : base(status, replyMessages)
    {
        Data = default;
    }
}
```

### `DataReplyStatus` Values

This enum contains the statuses I found useful in my projects. An independent implementation of the pattern can choose its own list. When using the packaged Artisan.Orm protocol, however, use the fixed enum names below: an unknown textual name is rejected by `DataReply.ParseStatus`. Express application-specific details through message `Code` values, or define a separate application result contract.

| Code | Usage |
|---|---|
| `Ok` | The default status when everything had executed as expected. |
| `Fail` | When a query goal was not achieved. |
| `Missing` | When a query did not find a record with the Id parameter. |
| `Validation` | When a query found a threat to data integrity. |
| `Concurrency` | When two or more users updated the same record at the same time. |
| `Denial` | When data access is calculated in the database, the user was not authorised to see the requested data, and you want to inform the user about the reasons. |
| `Error` | All unexpected errors and exceptions. |

### `DataReplyMessage` Properties

Messages are for delivering additional information about an exceptional case. `DataReplyMessage` has the following properties:

| Property | Usage |
|---|---|
| `Code` | A string identifier of a message. |
| `Id` | An integer identifier of the problem record in the database that caused an exceptional case. |
| `Text` | Any human-readable information for logs or other needs. |
| `Value` | A value that caused an exceptional case. |

A `DataReply` object has an array of `DataReplyMessage`s — enough for describing the exceptional-case details of any kind.

### How `DataReplyMessage`s Can Be Used

Imagine that a user submits a form with many fields. Client validation finds no errors, but server validation does — for example, server validation finds that the `Login` and `Email` are not unique. Then `DataReply` will have `Status = DataReplyStatus.Validation` and an array of `DataReplyMessage`s containing two items:

| Code | Id | Text | Value |
|---|---:|---|---|
| `NON_UNIQUE_LOGIN` | 15 | Login already exists | `Admin` |
| `NON_UNIQUE_EMAIL` | 15 | Email already exists | `admin@mail.com` |

A data service is able to log this exception, and the UI is able to handle it and use this info for error highlighting and appropriate actions.

For the built-in SQL message mapper, `Code` must be a non-null string. Its result set contains four columns in the order `Code`, `Text`, `Id`, `Value`; unused values still need nullable columns in those positions. This is the contract of that particular mapper, not a requirement that arbitrary manually mapped tables mirror every C# model property.

In-memory replies are more flexible: for example, `new DataReply("Saved")` creates an informational message with `Text` and no `Code`. Messages themselves may be absent, including on a status-only failure. JSON omission depends on the configured serializer, not just the historical DataMember attributes; Step 5 specifies the current example's settings.

## Going Through the Steps

### Step 1 — In a database, find an exceptional case and output the necessary data

Database operations may raise errors and throw `SqlException` in C# code. The SQL Server `RAISERROR` command can output `ErrorNumber`, `ErrorMessage`, `ServerState`. That is good but not enough — very often a client wants to know more details about an error.

In order to output an error's details in a `DataReply` format, we create a special user-defined table type for messages:

```sql
create type dbo.DataReplyMessageTableType as table
(
    Code    varchar(50)     not null,
    [Text]  nvarchar(4000)      null,
    Id      bigint              null,
    [Value] sql_variant         null,

    unique (Code, Id)
);
```

Then in a stored procedure, we can output a status of the exceptional case together with its details. As an example, here is a part of `SaveUser` with checks for data concurrency and validity:

> **Historical SQL fragment.** This is not a complete replacement for the current procedure. Its unconditional transaction/rollback statements assume the procedure owns the whole transaction. The [current SaveUser procedure](../Database/5.%20Procedures/Users/SaveUser.sql) records the entry `@@TRANCOUNT` and only commits or rolls back a transaction it started. Preserve that ownership when several repositories share an external transaction: a bare SQL rollback also rolls back an outer transaction. See [SQL Server rollback semantics](https://learn.microsoft.com/en-us/sql/t-sql/language-elements/rollback-transaction-transact-sql?view=sql-server-ver17#remarks).

```sql
declare
    @UserId             int             ,
    @Login              varchar(20)     ,
    @Name               nvarchar(50)    ,
    @Email              varchar(50)     ,
    @Concurrency        varchar(20)     = 'Concurrency',
    @Validation         varchar(20)     = 'Validation',
    @DataReplyStatus    varchar(20)     ,
    @DataReplyMessages  dbo.DataReplyMessageTableType;

begin transaction;

if exists -- concurrency
(
    select * from dbo.Users u with (tablockx, holdlock)
    inner join @User t on t.Id = u.Id and t.[RowVersion] <> u.[RowVersion]
)
begin
    select DataReplyStatus = @Concurrency;
    rollback transaction;
    return;
end;

begin -- validation
    begin -- check User.Login uniqueness
        select top 1
            @UserId = u.Id,
            @Login  = u.[Login]
        from
            dbo.Users u
            inner join @User t on t.[Login] = u.[Login] and t.Id <> u.Id;

        if @Login is not null
        begin
            set @DataReplyStatus = @Validation;
            insert into @DataReplyMessages
            select Code = 'NON_UNIQUE_LOGIN', 'Login is not unique', @UserId, @Login;
        end;
    end;

    begin -- check User.Email uniqueness
        select top 1
            @UserId = u.Id,
            @Email  = u.Email
        from
            dbo.Users u
            inner join @User t on t.Email = u.Email and t.Id <> u.Id;

        if @Email is not null
        begin
            set @DataReplyStatus = @Validation;
            insert into @DataReplyMessages
            select Code = 'NON_UNIQUE_EMAIL', 'User email is not unique', @UserId, @Email;
        end;
    end;

    select DataReplyStatus = @DataReplyStatus;

    if @DataReplyStatus is not null
    begin
        select * from @DataReplyMessages;
        rollback transaction;
        return;
    end;
end;

-- save the user
-- output the saved user
```

Note the **output pattern** for an exceptional case:

```sql
select DataReplyStatus = @DataReplyStatus;

if @DataReplyStatus is not null
begin
    select * from @DataReplyMessages;
    rollback transaction;
    return;
end;
```

In this historical fragment, SQL NULL represents success; a non-null status represents failure. The current helper also accepts an explicit `Ok` status. For a defined non-OK status, output an optional message result set, finish any transaction owned by the procedure, and return. On success, the payload follows the status directly; no empty message result set is required.

### Step 2 — In a repository, recognise the exceptional case and read the data about it

In the repository method, we follow the output pattern and throw `DataReplyException` with `Status` and `Messages` from the stored procedure. This current version uses the shared helper and supplies both parameters required by the repository's `dbo.SaveUser`:

```csharp
public User SaveUser(User user)
{
    return GetByCommand(cmd =>
    {
        cmd.UseProcedure("dbo.SaveUser");
        cmd.AddTableRowParam("@User", user);
        cmd.AddTableParam("@RoleIds", user.RoleIds);

        return cmd.GetByReader(dr =>
        {
            CheckForDataReplyException(dr);
            return dr.ReadTo<User>();
        });
    });
}
```

The above repository method is written with Artisan.Orm ADO.NET extension methods, but it can be rewritten to use the regular ADO.NET methods.

`CheckForDataReplyException` accepts `Ok` case-insensitively, as well as SQL NULL, empty, and whitespace-only status strings. It advances once to the payload on success; do not insert another `NextResult()` after it. Call it only for a procedure that emits the status header, since it would otherwise consume actual data. Send one defined enum name as the wire status. The 4.1.0 parser rejects undefined numeric values and comma-separated combinations, including `Fail, Missing`; defined numeric values remain accepted for compatibility. Status-only exceptions include the status in their message.

The separate `@RoleIds` TVP is significant: the current procedure uses it to replace the saved user's roles. Omitting a TVP passes an empty table, so omitting this parameter can clear existing role assignments. See the [current repository method](../Tests/DAL/Users/Repository.cs) and [SQL parameter rules](https://learn.microsoft.com/en-us/sql/t-sql/statements/create-procedure-transact-sql?view=sql-server-ver17#parameter_name).

The helper is synchronous. There is no `CheckForDataReplyExceptionAsync` in the current source, and `GetByReaderAsync` takes a synchronous projection. For fully asynchronous status/payload traversal, own the reader and use its async methods with the cancellation token.

### Step 3 — In a repository, throw an exception so that a data service can handle it the C# way

The `DataReplyException` in the above code is a custom exception with two additional properties: `Status` and `Messages`. This abbreviated historical declaration shows the idea; the [current class](../Artisan.Orm/DataReplyException.cs) also has a status-only constructor and other helpers:

```csharp
public class DataReplyException : Exception
{
    public DataReplyStatus    Status   { get; }      = DataReplyStatus.Error;
    public DataReplyMessage[] Messages { get; set; }

    public DataReplyException(DataReplyStatus status, DataReplyMessage[] messages)
    {
        Status   = status;
        Messages = messages;
    }
}
```

So the `SaveUser` repository method:

- in the **normal case**, returns a `User` object;
- in the **expected exceptional case**, throws `DataReplyException` with status and messages;
- in the **unexpected exceptional case**, propagates the underlying exception, such as a `SqlException`, mapping error, or conversion error.

### Step 4 — In a data service, get normal data or catch an exception, and create a universal data reply

A data service can translate expected repository outcomes into the application's response model. The current example below is a method on an application service named `UserSaveService`, with injected `repository` and `logger` dependencies. It logs unexpected exceptions on the server, returns a safe message, and lets cancellation propagate. The repository's existing sample service is separately named [`UserDataService`](../Tests/DataServices/UserDataService.cs), with a `Save` method. Its test-only `DataServiceBase` now propagates cancellation and safely handles absent/short stack traces, but still exposes diagnostic details in test replies. Use the safer application policy shown here when adapting the pattern.

```csharp
public DataReply<User> SaveUser(User user)
{
    try
    {
        var saved = repository.SaveUser(user);
        return new DataReply<User>(saved);
    }
    catch (DataReplyException ex)
    {
        // log exception here, if necessary
        return new DataReply<User>(ex.Status, ex.Messages);
    }
    catch (OperationCanceledException)
    {
        throw;
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Saving a user failed.");
        return new DataReply<User>(
            DataReplyStatus.Error, "SAVE_USER_FAILED", "The user could not be saved.");
    }
}
```

The above `SaveUser` method:

- in the normal case, returns a `DataReply` object with `Status = DataReplyStatus.Ok` and `Data = User`;
- in the expected exceptional case, returns `DataReply` with a `Status` from the `DataReplyStatus` enum and `Messages` from the stored procedure;
- in the unexpected exceptional case, logs the original exception and returns `DataReplyStatus.Error` with a safe client message;
- on cancellation, propagates the cancellation instead of returning a normally completed error reply.

The original example copied `Message` and part of `StackTrace` into the reply. Keep those diagnostics on the server. Its `Substring(0, 500)` also failed when a non-null trace was shorter than 500 characters. If a development-only log field needs truncation, use `trace == null ? null : trace.Substring(0, Math.Min(trace.Length, 500))`; do not expose that field in the response. Apply the same cancellation policy when adapting this service to async methods.

### Step 5 — In an ASP.NET Web API controller, serialise the data reply into JSON format

The original controller was an ASP.NET Web API example. A current ASP.NET Core controller derived from `ControllerBase` can call the `UserSaveService` from Step 4 through an injected `service`. This endpoint uses the following illustrative HTTP policy; choose mappings appropriate to your application's operations:

```csharp
[HttpPost]
public IActionResult SaveUser(User user)
{
    var reply = service.SaveUser(user);
    var httpStatus = reply.Status switch
    {
        DataReplyStatus.Ok          => StatusCodes.Status200OK,
        DataReplyStatus.Missing     => StatusCodes.Status404NotFound,
        DataReplyStatus.Validation  => StatusCodes.Status400BadRequest,
        DataReplyStatus.Concurrency => StatusCodes.Status409Conflict,
        DataReplyStatus.Denial      => StatusCodes.Status403Forbidden,
        DataReplyStatus.Fail        => StatusCodes.Status409Conflict,
        _                          => StatusCodes.Status500InternalServerError
    };
    return StatusCode(httpStatus, reply);
}
```

The following `Program.cs` configuration defines the JSON contract for these MVC controllers. It uses System.Text.Json, includes the message fields, writes member names and enum strings in camel case, and omits null values:

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.IncludeFields = true;
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.JsonSerializerOptions.Converters.Add(
        new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});
```

This is an explicit application choice: `IncludeFields` affects other public fields serialized by these controllers too. An application that prefers a narrower contract can map replies to its own response DTOs instead. With System.Text.Json defaults, DataReply message fields would be omitted and enum statuses would be numeric; the historical DataMember annotations do not provide this configuration. See [field serialization](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/fields).

The examples use one top-level reply object, without an extra `dataReply` wrapper. `WhenWritingNull` omits null `Data` and `Messages`, but retains non-null default values such as `DataReply<int>.Data == 0`. Message `Id` values are JSON numbers under this contract.

In the **normal case** (the User payload is abbreviated to show the envelope):

```json
{
    "status": "ok",
    "data":   { "id": 1, "name": "John Smith" }
}
```

In the **expected exceptional case** with `Status = DataReplyStatus.Validation`:

```json
{
    "status": "validation",
    "messages": [
        { "code": "NON_UNIQUE_LOGIN", "text": "Login is not unique",      "id": 1, "value": "admin" },
        { "code": "NON_UNIQUE_EMAIL", "text": "User email is not unique", "id": 1, "value": "admin@mail.com" }
    ]
}
```

In the **unexpected exceptional case** with `Status = DataReplyStatus.Error`:

```json
{
    "status": "error",
    "messages": [
        { "code": "SAVE_USER_FAILED", "text": "The user could not be saved." }
    ]
}
```

### Step 6 — In web-client data services, get the JSON, define the status of a data reply, take appropriate actions

Below is the example of JavaScript and AngularJS code for a web-client `dataService`. Because all data requests and replies go through a single `dataService` and its methods, it is easy to handle replies with certain statuses using a unified approach.

This block is retained as historical AngularJS code using the original response policy. Its HTTP-error branch does not process business errors returned with HTTP 4xx. Use the modern alternative below with the current controller's HTTP status policy.

```javascript
(function () {
    "use strict";

    angular.module("app")
        .service('dataService', function ($q, $http) {

            var error = { status: "error" };
            var allowedStatuses =
                ["ok", "fail", "validation", "missing", "concurrency", "denial"];

            this.save = function (url, savingObject) {
                var deferred = $q.defer();

                $http({
                    method: 'Post',
                    url:    url,
                    data:   savingObject
                })
                .success(function (reply, status, headers, config) {
                    if ($.inArray((reply || {}).status, allowedStatuses) > -1) {
                        deferred.resolve(angular.fromJson(reply));
                    } else {
                        showError(reply, status, headers, config);
                        deferred.resolve(error);
                    }
                })
                .error(function (data, status, headers, config) {
                    showError(data, status, headers, config);
                    deferred.resolve(error);
                });

                return deferred.promise;
            };

            function showError(data, status, headers, config) {
                // inform a user about an unexpected exceptional case
            }
        });
})();
```

> **Current fetch alternative.** This version matches Step 5's example HTTP policy, including validation details in HTTP 400 responses. Fetch does not reject merely because a response has an HTTP error status, so validate both the HTTP status and the reply. See [Using Fetch](https://developer.mozilla.org/en-US/docs/Web/API/Fetch_API/Using_Fetch).
>
> ```javascript
> const EXPECTED_HTTP = new Map([
>     ["ok", 200], ["fail", 409], ["validation", 400],
>     ["missing", 404], ["concurrency", 409], ["denial", 403]
> ]);
>
> async function save(url, payload, signal) {
>     try {
>         const res   = await fetch(url, {
>             method:  "POST",
>             headers: { "Content-Type": "application/json" },
>             body:    JSON.stringify(payload),
>             signal,
>         });
>         const reply = await res.json();
>
>         if (EXPECTED_HTTP.get(reply?.status) === res.status) return reply;
>
>         showError({ httpStatus: res.status, reply });
>         return { status: "error" };
>     } catch (err) {
>         if (signal?.aborted || err?.name === "AbortError") throw err;
>         showError(err);
>         return { status: "error" };
>     }
> }
> ```
>
> The shape — single client-side service, allow-list of expected statuses, central error sink — is unchanged.

### Step 7 — In SPA controllers, get a data reply, define its status, take appropriate actions

And finally, here is an example of how the `DataReply` can be handled in a web-client controller:

```javascript
function save() {
    userService.save('api/users', $scope.user)
        .then(function (dataReply) {
            if (dataReply.status === 'ok') {
                var savedUser = dataReply.data;
                $scope.user = savedUser;
            }
            else if (dataReply.status === 'validation') {
                var messages = dataReply.messages || [];
                if (messages.length === 0) {
                    // show a general validation message
                }
                for (var i = 0; i < messages.length; i++) {
                    if (messages[i].code === 'NON_UNIQUE_LOGIN') {
                        // highlight the login UI control
                    }
                    else if (messages[i].code === 'NON_UNIQUE_EMAIL') {
                        // highlight the email UI control
                    }
                }
            }
            else if (dataReply.status === 'concurrency') {
                // message about concurrency
            }
        });
}
```

## Conclusion

The DataReply idea was dictated by the urgent need to transmit more details about exceptional cases to a web client during complex object-graph saving.

While saving a complex object, inconsistency or other problems may occur in any part of it. The task to collect and deliver all the possible issues about data at once required an appropriate transport. `DataReply` became such a solution.

Having tried this idea on several real projects, it can be said that it has proved its universality, effectiveness and right to life. ☺

## DataReply in Artisan.Orm

`DataReply`, `DataReplyStatus` enum, `DataReplyMessage` and `DataReplyException` are part of Artisan.Orm:

- [`Artisan.Orm/DataReply.cs`](../Artisan.Orm/DataReply.cs)
- [`Artisan.Orm/DataReplyException.cs`](../Artisan.Orm/DataReplyException.cs)
- [`Artisan.Orm/DataReplyMessage.cs`](../Artisan.Orm/DataReplyMessage.cs)
- [`Artisan.Orm/DataReplyState.cs`](../Artisan.Orm/DataReplyState.cs) — the `DataReplyStatus` enum

`RepositoryBase.CheckForDataReplyException(SqlDataReader)` reads the leading status result set. A parsed non-OK status throws `DataReplyException`, with optional messages. `Ok`, SQL NULL, empty, and whitespace-only strings advance once to the next result set so the caller can read the actual data. The explicit-`Ok` fix is part of the source being prepared for 4.1.0; do not assume it is present in an earlier installed package.

The Artisan.Orm wiki has a focused API-reference page: [Artisan Way of DataReply](https://github.com/lobodava/artisan-orm/wiki/Artisan-Way-of-Data-Reply). This article is the long-form story behind that wiki page.

## About the Source Code

The original article shipped with a Visual Studio 2015 solution containing the Artisan.Orm DLL, an SSDT `Database` project, and a `Tests` project. That code now lives in the Artisan.Orm GitHub repository — current source is at [github.com/lobodava/artisan-orm](https://github.com/lobodava/artisan-orm) and the released library is on [NuGet](https://www.nuget.org/packages/Artisan.ORM/).

## Original Publication History

- **16 April 2017** — initial publication. Artisan.Orm source code at version 1.1.0.
- **10 June 2021** — final revision on CodeProject.

---

*Originally published as [Artisan Way of Data Reply](https://www.codeproject.com/Articles/1181182/Artisan-Way-of-Data-Reply) on CodeProject, April 2017, with revisions through June 2021. CodeProject went read-only in late 2024; this version preserves the article for posterity. Re-published under [CC-BY-SA-4.0](LICENSE).*
