# [![Artisan.Orm Logo](https://raw.githubusercontent.com/lobodava/artisan-orm/master/Logo.png)](http://www.nuget.org/packages/Artisan.ORM) Artisan.ORM

ADO.NET Micro-ORM to SQL Server, implemented as .NET Standard 2.1 (since version 3.5.x) or a .Net Core 6.0 library (since version 3.0.0).
Use version 2.x.x, which was built with Net Standard 2.0, if you want to utilise this library with the .Net Framework or a previous version of .NET Core.

## ADO.NET Micro-ORM to SQL Server.

First there was a desire to save a graph of objects for one access to the database: 
* one command on the client,
* one request to the application server,
* one access to the database.

Thus the method of [How to Save Object Graph in Master-Detail Relationship with One Stored Procedure](https://www.codeproject.com/Articles/1153556/How-to-Save-Object-Graph-in-Master-Detail-Relation) was found.

Then there was a desire of more control over Object-Relational Mapping, better performance and ADO.NET code reduction.

Thus a set of extensions to ADO.NET methods turned into a separate project. Here is a story about [Artisan.ORM or How To Reinvent the Wheel](https://www.codeproject.com/articles/1155836/artisan-orm-or-how-to-reinvent-the-wheel)!

Finally the *object graph saving method* required a new approach to transmitting of more details about exceptional cases. [Artisan Way of Data Reply](https://www.codeproject.com/Articles/1181182/Artisan-Way-of-Data-Reply) became such an answer.  

## What to read for better understanding

Full information about Artisan.ORM is available in [documentation Wiki](https://github.com/lobodava/artisan-orm/wiki). 

The most interesting articles from Wiki are:

* [The Sample](https://github.com/lobodava/artisan-orm/wiki/The-Sample)
* [Getting Started](https://github.com/lobodava/artisan-orm/wiki/Getting-started)
* [Read Methods Understanding](https://github.com/lobodava/artisan-orm/wiki/Read-Methods-Understanding)
* [Mappers](https://github.com/lobodava/artisan-orm/wiki/Mappers)
* [cmd.AddTableParam](https://github.com/lobodava/artisan-orm/wiki/cmd.AddTableParam)
* [Code Generation](https://github.com/lobodava/artisan-orm/wiki/Code-Generation)


## Some propositions, statements and additional information

Artisan.ORM was created to meet the following requirements:
* interactions with database should mostly be made through *stored procedures*;
* all calls to database should be encapsulated into *repository methods*;
* a *repository method* should be able to read or save a *complex object graph* with one *stored procedure*;
* it should work with the highest possible performance, even at the expense of the convenience and development time.

To achieve these goals Artisan.ORM uses:
* the `SqlDataReader` as the fastest method of data reading;
* a bunch of its own extensions to ADO.NET [SqlCommand](https://github.com/lobodava/artisan-orm/wiki/SqlCommand-Extensions) and [SqlDataReader](https://github.com/lobodava/artisan-orm/wiki/SqlDataReader-extentions) methods, both synchronous and asynchronous;
* strictly structured static [Mappers](https://github.com/lobodava/artisan-orm/wiki/Mappers);
* [user-defined table types](https://github.com/lobodava/artisan-orm/wiki/User-Defined-Table-Types) as a mean of object saving;
* [unique negative identities](https://github.com/lobodava/artisan-orm/blob/master/Artisan.Orm/NegativeIdentity.cs) as a flag of new entities;
* a [special approach](https://github.com/lobodava/artisan-orm/wiki/Negative-identities-and-object-graph-saving) to writing stored procedures for object reading and saving.

Artisan.ORM is available as [NuGet Package](http://www.nuget.org/packages/Artisan.ORM).

More examples of the Artisan.ORM usage are available in the [Tests](https://github.com/lobodava/artisan-orm/tree/master/Tests) and [Database](https://github.com/lobodava/artisan-orm/tree/master/Database) projects.
