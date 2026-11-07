using System.Data;
using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tests.DAL.Folders;
using Tests.DAL.Folders.Models;

namespace Tests.Tests;

[TestClass]
public class FolderSqlRegressionTest
{
    [TestMethod]
    public void ReparentWithRecalculationRenumbersBothParentsAndTheirDescendants()
    {
        using var repository = new Repository(new AppSettings().ConnectionStrings.DatabaseConnection);
        repository.BeginTransaction(IsolationLevel.Serializable, _ =>
        {
            var root = repository.GetUserRootFolder(14);
            var container = SaveChild(repository, root, "SqlRegression_" + Guid.NewGuid().ToString("N"));
            var oldParent = SaveChild(repository, container, "A");
            var newParent = SaveChild(repository, container, "B");

            // Both parents retain children. Their names interleave in the combined anchor
            // result, so global numbering cannot accidentally satisfy per-parent assertions.
            var oldA = SaveChild(repository, oldParent, "A");
            var movingB = SaveChild(repository, oldParent, "B");
            var oldD = SaveChild(repository, oldParent, "D");
            var newC = SaveChild(repository, newParent, "C");
            var newE = SaveChild(repository, newParent, "E");
            var movingChild = SaveChild(repository, movingB, "Leaf");
            var movingGrandchild = SaveChild(repository, movingChild, "Grandchild");
            var oldDChild = SaveChild(repository, oldD, "OldLeaf");
            var newCChild = SaveChild(repository, newC, "NewLeaf");

            var fixtures = new[]
            {
                container, oldParent, newParent, oldA, movingB, oldD, newC, newE,
                movingChild, movingGrandchild, oldDChild, newCChild
            };
            var movingId = movingB.Id;
            movingB.ParentId = newParent.Id;

            var saved = repository.SaveFolder(movingB, withHidReorder: true);
            var actual = repository.GetFolderWithSubFolders(container.Id);

            Assert.AreEqual(movingId, saved.Id);
            Assert.AreEqual(newParent.Id, saved.ParentId);
            Assert.AreEqual(fixtures.Length, actual.Count);
            var actualById = actual.ToDictionary(folder => folder.Id);
            foreach (var expected in fixtures)
            {
                Assert.IsTrue(actualById.TryGetValue(expected.Id, out var folder), "The move must preserve every folder ID.");
                Assert.AreEqual(expected.Name, folder.Name);
                Assert.AreEqual(expected.ParentId, folder.ParentId);
            }

            foreach (var parent in actual)
            {
                var children = actual.Where(folder => folder.ParentId == parent.Id)
                    .OrderBy(folder => folder.Name, StringComparer.Ordinal).ThenBy(folder => folder.Id).ToArray();
                for (var index = 0; index < children.Length; index++)
                {
                    var expectedPath = parent.HidPath + (index + 1).ToString(CultureInfo.InvariantCulture) + "/";
                    Assert.AreEqual(expectedPath, children[index].HidPath,
                        "Every sibling group must have contiguous paths under its actual parent.");
                    Assert.AreEqual(parent.Level + 1, (int)children[index].Level);
                }
            }

            Assert.AreEqual(actualById[movingId].HidPath, saved.HidPath);
            Assert.AreEqual(2, actual.Count(folder => folder.ParentId == oldParent.Id));
            Assert.AreEqual(3, actual.Count(folder => folder.ParentId == newParent.Id));
            // No commit: the repository rolls back this fixture, including on assertion failure.
        });
    }

    [TestMethod]
    public void HidCodePreservesCompleteDistinctHexForLongHierarchyIds()
    {
        var prefix = "/" + string.Concat(Enumerable.Repeat("1/", 1100));
        using var connection = OpenConnection();
        using var command = new SqlCommand("""
            SELECT DATALENGTH(CAST(item.Hid AS varbinary(892))) AS BinaryBytes,
                   dbo.GetHidCode(item.Hid) AS ActualCode,
                   CONVERT(varchar(1784), CAST(item.Hid AS varbinary(892)), 2) AS ExpectedCode
            FROM (VALUES (1, hierarchyid::Parse(@FirstPath)),
                         (2, hierarchyid::Parse(@SecondPath))) item(Ordinal, Hid)
            ORDER BY item.Ordinal;
            """, connection);
        command.Parameters.Add("@FirstPath", SqlDbType.NVarChar, -1).Value = prefix + "1/";
        command.Parameters.Add("@SecondPath", SqlDbType.NVarChar, -1).Value = prefix + "2/";
        using var reader = command.ExecuteReader();
        var actualCodes = new List<string>();
        while (reader.Read())
        {
            var binaryBytes = reader.GetInt32(0);
            var actual = reader.GetString(1);
            var expected = reader.GetString(2);
            Assert.IsTrue(binaryBytes > 500);
            Assert.AreEqual(2 * binaryBytes, actual.Length);
            Assert.AreEqual(expected, actual);
            actualCodes.Add(actual);
        }

        Assert.AreEqual(2, actualCodes.Count);
        Assert.AreNotEqual(actualCodes[0], actualCodes[1]);
        Assert.IsTrue(string.CompareOrdinal(actualCodes[0], actualCodes[1]) < 0);
    }

    [TestMethod]
    public void HidCodeRetainsNullRootAndShortPathBehavior()
    {
        using var connection = OpenConnection();
        using var command = new SqlCommand("""
            SELECT dbo.GetHidCode(CAST(NULL AS hierarchyid)),
                   dbo.GetHidCode(hierarchyid::GetRoot()),
                   dbo.GetHidCode(hierarchyid::Parse('/1/')),
                   dbo.GetHidCode(hierarchyid::Parse('/1/1/'));
            """, connection);
        using var reader = command.ExecuteReader();
        Assert.IsTrue(reader.Read());
        Assert.IsTrue(reader.IsDBNull(0));
        Assert.AreEqual(string.Empty, reader.GetString(1));
        Assert.AreEqual("58", reader.GetString(2));
        Assert.AreEqual("5AC0", reader.GetString(3));
    }

    private static Folder SaveChild(Repository repository, Folder parent, string name) =>
        repository.SaveFolder(new Folder { ParentId = parent.Id, Name = name }, withHidReorder: false);

    private static SqlConnection OpenConnection()
    {
        var connection = new SqlConnection(new AppSettings().ConnectionStrings.DatabaseConnection);
        connection.Open();
        return connection;
    }
}
