using SoccerTournament.Application.Abstractions.Pagination;
using SoccerTournament.Infrastructure.ReadSide.Sql;

namespace SoccerTournament.Infrastructure.Tests.ReadSide;

public class SqlBuildingTests
{
    private static readonly SortMap Sort = new(
        new Dictionary<string, string> { ["fullName"] = "t.full_name", ["createdAt"] = "t.created_at" },
        defaultOrderBy: "t.full_name ASC",
        tieBreaker: "t.uuid ASC");

    [Theory]
    [InlineData("fullName", SortDirection.Asc, "ORDER BY t.full_name ASC, t.uuid ASC")]
    [InlineData("FULLNAME", SortDirection.Desc, "ORDER BY t.full_name DESC, t.uuid ASC")]
    [InlineData("createdAt", SortDirection.Desc, "ORDER BY t.created_at DESC, t.uuid ASC")]
    [InlineData(null, SortDirection.Desc, "ORDER BY t.full_name ASC, t.uuid ASC")]
    public void SortMap_ShouldTranslatePublicFieldToWhitelistedColumn(string? sortBy, SortDirection direction, string expected)
    {
        Assert.Equal(expected, Sort.ToOrderByClause(sortBy, direction));
    }

    [Fact]
    public void SortMap_WithInjectionAttempt_ShouldNeverConcatenateInput()
    {
        var orderBy = Sort.ToOrderByClause("full_name; DROP TABLE teams--", SortDirection.Asc);

        Assert.Equal("ORDER BY t.full_name ASC, t.uuid ASC", orderBy);
        Assert.DoesNotContain("DROP", orderBy);
    }

    [Fact]
    public void SqlFilter_ShouldOnlyIncludeConditionsThatApply()
    {
        var filter = new SqlFilter()
            .Add(true, "t.full_name LIKE @Name", "Name", "%club%")
            .Add(false, "t.founded_year >= @From", "From", null);

        Assert.Equal("WHERE t.full_name LIKE @Name", filter.ToWhereClause());
        Assert.Contains("Name", filter.Parameters.ParameterNames);
        Assert.DoesNotContain("From", filter.Parameters.ParameterNames);
    }

    [Fact]
    public void SqlFilter_WithoutConditions_ShouldReturnEmptyWhere()
    {
        Assert.Equal(string.Empty, new SqlFilter().ToWhereClause());
    }

    [Theory]
    [InlineData("club", "%club%")]
    [InlineData("  50%  ", "%50[%]%")]
    [InlineData("a_b", "%a[_]b%")]
    [InlineData("[x]", "%[[]x]%")]
    public void ContainsPattern_ShouldEscapeSqlWildcards(string input, string expected)
    {
        Assert.Equal(expected, SqlFilter.ContainsPattern(input));
    }
}
