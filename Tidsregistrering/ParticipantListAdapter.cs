using Android.Content;
using Android.Views;
using Android.Widget;
using Tidsregistrering.Models;

namespace Tidsregistrering;

public sealed class ParticipantListAdapter(Context context, IReadOnlyList<Participant> participants) : BaseAdapter<Participant>
{
    public override int Count => participants.Count;
    public override Participant this[int position] => participants[position];
    public override long GetItemId(int position) => position;

    public override View GetView(int position, View? convertView, ViewGroup? parent)
    {
        var view = convertView ?? LayoutInflater.From(context)!.Inflate(Android.Resource.Layout.SimpleListItem2, parent, false)!;
        var participant = participants[position];
        view.FindViewById<TextView>(Android.Resource.Id.Text1)!.Text = $"{participant.FirstName} {participant.Surname}";
        view.FindViewById<TextView>(Android.Resource.Id.Text2)!.Text = participant.DisplayBirthDate;
        return view;
    }
}
