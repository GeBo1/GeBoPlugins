using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using GeBoCommon.AutoTranslation;
using GeBoCommon.Chara;
#if AI || HS2
using AIChara;

#endif

namespace GeBoCommon
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "UnusedMemberInSuper.Global")]
    public interface IGeBoAPI
    {
        /// <summary>
        /// Gets the automatic translation helper.
        /// </summary>
        /// <value>
        /// The automatic translation helper.
        /// </value>
        IAutoTranslationHelper AutoTranslationHelper { get; }

        /// <summary>
        /// Enumerate names from ChaFile
        /// </summary>
        /// <param name="chaFile"/>
        /// <returns>Names as KeyValuePairs (key is index, value is name string)</returns>
        IEnumerable<KeyValuePair<int, string>> ChaFileEnumerateNames(ChaFile chaFile);

        /// <summary>
        /// Gets the full name from a ChaFile
        /// </summary>
        /// <param name="chaFile"/>
        /// <returns>full name</returns>
        string ChaFileFullName(ChaFile chaFile);

        /// <summary>
        /// Sets name at given index on a ChaFile
        /// </summary>
        /// <param name="chaFile"/>
        /// <param name="index">index of name to set</param>
        /// <param name="chaName">new name value</param>
        void ChaFileSetName(ChaFile chaFile, int index, string chaName);

        /// <summary>
        /// Plays the notification.
        /// </summary>
        /// <param name="notificationSound">The notification sound to play.</param>
        void PlayNotification(NotificationSound notificationSound);

        /// <summary>
        /// Gets the index for the type of name given
        /// </summary>
        /// <param name="name">Type of name.</param>
        /// <returns>index</returns>
        int ChaFileNameToIndex(string name);

        /// <summary>
        /// Gets the string representing the type of name at the given index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>type of name</returns>
        string ChaFileIndexToName(int index);

        /// <summary>
        /// Gets name type for a given name index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        NameType ChaFileIndexToNameType(int index);

        /// <summary>
        /// Gets the number of names stored per ChaFile for this game.
        /// </summary>
        /// <value>
        /// name count
        /// </value>
        int ChaFileNameCount { get; }

        /// <summary>
        /// Gets the names types supported by this game.
        /// </summary>
        /// <value>
        /// name types
        /// </value>
        IList<string> ChaFileNames { get; }
    }
}
