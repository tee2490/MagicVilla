namespace MagicVilla_ClassLibrary.Utility
{
    public static class SD
    {
        public enum ApiType
        {
            GET,
            POST,
            PUT,
            DELETE
        }

        public static string AccessToken = "JWTToken";

        public const string Admin = "admin";
        public const string Customer = "customer";
    }
}
