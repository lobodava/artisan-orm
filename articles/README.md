# Articles

This folder contains a series of articles about the design patterns behind Artisan.Orm. They were originally published on [CodeProject](https://www.codeproject.com/) between 2016 and 2021. CodeProject went read-only in late 2024, leaving local PDFs as the only surviving copies; the articles have been re-published here from those PDFs.

The articles are licensed under **[CC-BY-SA-4.0](LICENSE)** — share, remix, and build upon them with attribution. The Artisan.Orm source code in the rest of the repository remains under the [MIT License](../Artisan.Orm/LICENSE.txt).

The original publication dates and design narrative are preserved. Compatibility notes and clearly identified current examples describe the source being prepared for **4.1.0**; this does not mean that release has already been published. Historical class declarations, SQL fragments, and web-framework examples are not standalone copies of the current implementation. For installation and current APIs, start with the [project README](../README.md), [Getting Started](https://github.com/lobodava/artisan-orm/wiki/Getting-Started), and [4.1.0 changes](../CHANGELOG.md).

## The series

| # | Article | Original date |
|---|---|---|
| 1 | [How to Save an Object Graph in Master-Detail Relationship with One Stored Procedure](01-object-graph-saving.md) | Nov 2016 (revised May 2023) |
| 2 | [Artisan.Orm — How to Reinvent the Wheel](02-artisan-orm.md) | Nov 2016 (revised Jun 2020) |
| 3 | [The Artisan Way of Data Reply](03-data-reply.md) | Apr 2017 (revised Jun 2021) |
| 4 | [Combination of Id-ParentId and HierarchyId Approaches to Hierarchical Data](04-hierarchyid-combination.md) | 17 Jul 2017 |

## Reading order

The articles can be read independently, but they were written as a progression:

1. **Article 1** introduces the core problem — saving a master-detail object graph through one stored procedure — and the *negative-identities* technique that makes it work.
2. **Article 2** generalises the patterns from Article 1 into a small ADO.NET helper library: Artisan.Orm.
3. **Article 3** adds the *DataReply* protocol for delivering structured business-logic outcomes from stored procedures back to the client.
4. **Article 4** extends the model with `hierarchyid` for cases where the data is itself hierarchical.

For the design story, [Article 1](01-object-graph-saving.md) is the entry point. Use the current setup and API documentation linked above when implementing an application.

## Relationship to the wiki

The [wiki](https://github.com/lobodava/artisan-orm/wiki) is the **reference documentation** for the library — short pages with the API of each method, organised for quick lookup. These articles are the **long-form narrative** behind the design — the *why* and the *how it evolved*. Where it makes sense, individual wiki pages link back to the corresponding article for the deeper story.

Article 3 includes current DataReply protocol, JSON serialization, and HTTP integration examples alongside its historical discussion. DataReply is optional, and manually authored mappers can deliberately use a SQL table shape different from the C# model; the articles do not impose a universal one-property-per-column contract.
