using System.Collections.Generic;
using System.Linq;
using StorageNetwork.Components;

namespace StorageNetwork.UI
{
    internal static class StorageNetworkEnrollableWindowSignature
    {
        public static string Build(
            IEnumerable<StorageNetworkEnrollment> enrollments,
            int worldFilterId,
            string searchText)
        {
            string worldSignature = worldFilterId.ToString();
            string searchSignature = searchText ?? string.Empty;
            return worldSignature + ":" + searchSignature + "|" + string.Join("|", (enrollments ?? Enumerable.Empty<StorageNetworkEnrollment>())
                .OrderBy(enrollment => enrollment != null ? enrollment.GetInstanceID() : 0)
                .Select(enrollment =>
                {
                    Storage storage = enrollment != null ? enrollment.GetComponent<Storage>() : null;
                    Studyable studyable = enrollment != null ? enrollment.GetComponent<Studyable>() : null;
                    return string.Format("{0}:{1}:{2}:{3:0.###}:{4:0.###}:{5}",
                        enrollment != null ? enrollment.GetInstanceID() : 0,
                        enrollment != null ? enrollment.IncludedInSceneNetwork : false,
                        enrollment != null ? enrollment.gameObject.GetProperName() : string.Empty,
                        storage != null ? storage.MassStored() : 0f,
                        storage != null ? storage.Capacity() : 0f,
                        studyable != null && studyable.Studied);
                }));
        }
    }
}
