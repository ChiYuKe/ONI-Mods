using System.Collections.Generic;
using System.Text;
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
            List<StorageNetworkEnrollment> ordered =
                new List<StorageNetworkEnrollment>();
            if (enrollments != null)
            {
                foreach (StorageNetworkEnrollment enrollment in enrollments)
                {
                    if (enrollment != null)
                    {
                        ordered.Add(enrollment);
                    }
                }
            }

            ordered.Sort((left, right) =>
                left.GetInstanceID().CompareTo(right.GetInstanceID()));
            StringBuilder builder = new StringBuilder(64 + ordered.Count * 32);
            builder.Append(worldFilterId)
                .Append(':')
                .Append(searchText ?? string.Empty);
            foreach (StorageNetworkEnrollment enrollment in ordered)
            {
                builder.Append('|')
                    .Append(enrollment.GetInstanceID())
                    .Append(':')
                    .Append(enrollment.gameObject.GetProperName());
            }

            return builder.ToString();
        }
    }
}
