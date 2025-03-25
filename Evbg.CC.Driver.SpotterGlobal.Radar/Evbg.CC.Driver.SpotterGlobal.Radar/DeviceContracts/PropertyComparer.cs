
namespace Evbg.CC.Driver.SpotterGlobal.Radar
{
    public static class PropertyComparer
    {
        /// <summary>
        /// Helper method comparing the values of two variables and return the newValue if it's different from the oldValue
        /// </summary>
        /// <typeparam name="T">The value Type</typeparam>
        /// <param name="oldValue">Old value</param>
        /// <param name="newValue">New value</param>
        /// <param name="areDifferent">Boolean flag which is set to True if the newValue is different from the oldValue</param>
        /// <returns>the newValue</returns>
        public static T CompareValues<T>(T oldValue, T newValue, ref bool areDifferent)
        {
            if (!oldValue?.Equals(newValue) ?? true)
            {
                areDifferent = true;
            }

            return newValue;
        }
    }
}