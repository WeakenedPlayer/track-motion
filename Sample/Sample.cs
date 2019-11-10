using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using ScriptPortal.Vegas;

namespace Sample
{
	public class EntryPoint
	{
		public void FromVegas(Vegas vegas)
		{
			// ----------------------------------------------------------------
			// 対象トラックの確認
			// ----------------------------------------------------------------
			VideoTrack track = FindSelectedTrack(vegas.Project.Tracks);
			if (track==null)
			{
				return;
			}
			
			// トラックが選択されていないか、ビデオトラックではない
			if (track == null) {
				MessageBox.Show("ビデオトラックを選択してください。");
				return;
			}

			// 選択されたトラックイベントがあるか
			TrackEvents events = track.Events;
			if (events.Count == 0)
			{
				MessageBox.Show("トラックにビデオが含まれていません。");
				return;
			}

			// ----------------------------------------------------------------
			// 座標データを含むファイルの選択
			// ----------------------------------------------------------------
			OpenFileDialog ofd = new OpenFileDialog();
			ofd.CheckFileExists = true;
			ofd.CheckPathExists = true;
			if (ofd.ShowDialog() != DialogResult.OK)
			{
				MessageBox.Show("読み込みを中止します。");
				return;
			}
			
			// ----------------------------------------------------------------
			// [ long tick, float x, float y ] のデータを読み込む準備をする
			// ----------------------------------------------------------------
			BinaryReader reader = new BinaryReader(File.OpenRead(ofd.FileName));
			const long dataPerRow = sizeof(long) + sizeof(float) + sizeof(float);
			long recordCount = reader.BaseStream.Length / dataPerRow;
			long last = DateTime.Now.ToBinary();

			// ----------------------------------------------------------------
			// トラック内のビデオがある期間だけトラックモーションを読み込む
			// ----------------------------------------------------------------
			track.TrackMotion.MotionKeyframes.Clear();

			long skip = 0;
			double nearestStart = 0;
			double nearestEnd = 0;
			double farthestEnd = FindFarthestEnd(track, 0);

			for (long i = 0; i < recordCount; i++)
			{
				// 読み込み
				double current = (double)(reader.ReadInt64() / 10000); // 1tick = 100ns
				double nx = 1920 * (reader.ReadSingle() - 0.5f);
				double ny = 1080 * (0.5f - reader.ReadSingle());

				if (current >= nearestEnd)
				{
					nearestStart = FindNearestStart(track, current);
					nearestEnd = FindNearestEnd(track, current);
				}

				if (current > farthestEnd) {
					break;
				}

				// レンジ内か確認する。
				if (current >= nearestStart && current <= nearestEnd)
				{
					skip++;
					if (skip > 2) {
						skip = 0;
						foreach (TrackEvent evt in events)
						{

							// キーフレームを追加し、座標をセット…点数が多いとクラッシュする模様。
							TrackMotionKeyframe frame = track.TrackMotion.InsertMotionKeyframe(new Timecode(current));
							frame.PositionX = nx;
							frame.PositionY = ny;
						}
					}
				}
			}
			reader.Close();
		}

		VideoTrack FindSelectedTrack(Tracks tracks)
		{
			foreach (Track track in tracks)
			{
				if (track.Selected && track.IsVideo())
				{
					return (VideoTrack)track;
				}
			}
			return null;
		}

		List<TrackEvent> FindSelectedTrackEvent(Track track)
		{
			List<TrackEvent> list = new List<TrackEvent>(0);

			foreach (TrackEvent evt in track.Events)
			{
				if (evt.Selected)
				{
					list.Add(evt);
				}
			}
			return list;
		}

		double FindNearestStart(Track track, double current) 
		{
			double start = Double.MaxValue;
			foreach (TrackEvent evt in track.Events)
			{
				double evtStart = evt.Start.ToMilliseconds();
				if ( evtStart > current && evtStart < start )
				{
					start = evtStart;
				}
			}
			return start;
		}

		double FindNearestEnd(Track track, double current)
		{
			double end = Double.MaxValue;
			foreach (TrackEvent evt in track.Events)
			{
				double evtEnd = evt.End.ToMilliseconds();
				if (evtEnd > current && evtEnd < end)
				{
					end = evtEnd;
				}
			}
			return end;
		}

		double FindFarthestEnd(Track track, double current)
		{
			double end = 0;
			foreach (TrackEvent evt in track.Events)
			{
				double evtEnd = evt.End.ToMilliseconds();
				if (evtEnd > current && evtEnd > end)
				{
					end = evtEnd;
				}
			}
			return end;
		}
	}
}
