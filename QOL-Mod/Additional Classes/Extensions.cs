namespace QOL;

public static class Extensions
{
    public static string Replace(this string origStr, char replaceableChar, string replacedWith) 
        => origStr.Replace(replaceableChar.ToString(), replacedWith);

    public static bool StartsWith(this string str, char charToFind)
        => str.StartsWith(charToFind.ToString());
}