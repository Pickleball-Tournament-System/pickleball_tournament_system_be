// (Trong file VnPayHelper.cs)
using System.Globalization;

public class VnPayCompare : IComparer<string>
{
    // Sửa 'string' thành 'string?'
    public int Compare(string? x, string? y)
    {
        // Logic cũ của bạn (hoặc logic này) là ổn
        if (x == y) return 0;
        if (x == null) return -1;
        if (y == null) return 1;
        var vnpCompare = CompareInfo.GetCompareInfo("en-US");
        return vnpCompare.Compare(x, y, CompareOptions.Ordinal);
    }
}