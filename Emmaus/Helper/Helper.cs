namespace Emmaus.Helper
{
    public static class Helper
    {
        public static readonly string Nbsp = "&nbsp;";

        public static string ReplaceWhitespaceWithNbsp(this string inputString)
        {
            string newString = string.Empty;
            foreach (var item in inputString)
            {
                if (char.IsWhiteSpace(item))
                {
                    newString += Nbsp;
                }
                else
                {
                    newString += item;
                }
            }
            return newString;
        }
    }
}