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
			// [1] スクリプト起動時に選択していたビデオトラックを取得
			// ----------------------------------------------------------------
			VideoTrack track = FindSelectedTrack(vegas.Project.Tracks);
			if (track == null)
			{
				return;
			}

			// ビデオトラックが無ければ中断
			if (track == null)
			{
				MessageBox.Show("ビデオトラックを選択してください。");
				return;
			}

			// ビデオトラックにトラックイベントが無ければ中断
			TrackEvents events = track.Events;
			if (events.Count == 0)
			{
				MessageBox.Show("トラックにビデオが含まれていません。");
				return;
			}

			// ----------------------------------------------------------------
			// [2] 座標データを含むバイナリファイルを選択する
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
			// [3] 座標データ( long tick, float x, float y )を
			// 　　を読み込むストリームを開く
			// ----------------------------------------------------------------
			BinaryReader reader = new BinaryReader(File.OpenRead(ofd.FileName));

			// 座標データ1つあたりのサイズ【座標データの形式によって変える】
			const long dataPerRow = sizeof(long) + sizeof(float) + sizeof(float);

			// 座標データ数
			long recordCount = reader.BaseStream.Length / dataPerRow;

			// ----------------------------------------------------------------
			// [4] トラック内のトラックイベントがある期間のトラックモーションを読み込む
			// ----------------------------------------------------------------
			// ビデオトラックのトラックモーションをクリアする
			track.TrackMotion.MotionKeyframes.Clear();

			// 座標データの間引き【お好みで変える】
			const long skipCount = 3;                           // 間引き数
			long skip = 0;                                      // 間引き数カウンタ

			// 最後のトラックイベントの情報
			double farthestEnd = FindLastEventEnd(track);       // 終了時刻[ms]

			// 直近のトラックイベントの情報
			double nearestStart = 0;                            // 開始時刻[ms]
			double nearestEnd = 0;                              // 終了時刻[ms]

			// 座標データ数だけ以下を繰り返す
			for (long i = 0; i < recordCount; i++)
			{
				// 座標データの読み込み 【座標データの形式によって変える】
				double nt = (double)(reader.ReadInt64() / 10000); // 時間変換(決め打ち: 1tick = 100ns から msに変換)
				double nx = 1920 * (reader.ReadSingle() - 0.5f);  // 座標変換(決め打ち: [0,1]を[-960, 960]に変換)
				double ny = 1080 * (0.5f - reader.ReadSingle());  // 座標変換(決め打ち: [0,1]を[540,-540]に変換)

				// [4-A] 現在時刻ntが最後のトラックイベントの終了時刻を超えた時
				if (nt > farthestEnd)
				{
					// 座標データを読み込む必要がないため、ループを抜ける。
					break;
				}

				// [4-B] 現在時刻が、直近のトラックイベントの終了時刻を超えた時
				if (nt > nearestEnd)
				{
					// 間引きカウンタリセット
					skip = 0;

					// 直近のトラックイベントの開始・終了時刻を更新する
					nearestStart = FindNearestEventStart(track, nt);
					nearestEnd = FindNearestEventEnd(track, nt);
				}

				// [4-C] 現在時刻ntが直近のトラックイベントの範囲内の時
				if (nt >= nearestStart && nt <= nearestEnd)
				{
					// データを間引く
					skip--;
					if (skip <= 0)
					{
						skip = skipCount;

						// キーフレームを追加し、座標をセットする
						TrackMotionKeyframe frame = track.TrackMotion.InsertMotionKeyframe(new Timecode(nt));
						frame.PositionX = nx;
						frame.PositionY = ny;
					}
				}
			}

			// [5] ストリームを閉じる
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

		// 以下あまり賢くないつくりの関数

		// ----------------------------------------------------------------
		// currentで与えた時刻に一番近いトラックイベントの開始時刻を取得
		// ----------------------------------------------------------------
		double FindNearestEventStart(Track track, double current)
		{
			double start = Double.MaxValue;
			foreach (TrackEvent evt in track.Events)
			{
				double evtStart = evt.Start.ToMilliseconds();
				if (evtStart > current && evtStart < start)
				{
					start = evtStart;
				}
			}
			return start;
		}

		// ----------------------------------------------------------------
		// currentで与えた時刻に一番近いトラックイベントの終了時刻を取得
		// ----------------------------------------------------------------
		double FindNearestEventEnd(Track track, double current)
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

		// ----------------------------------------------------------------
		// 最後のトラックイベントの終了時刻を取得
		// ----------------------------------------------------------------
		double FindLastEventEnd(Track track)
		{
			double end = 0;
			foreach (TrackEvent evt in track.Events)
			{
				double evtEnd = evt.End.ToMilliseconds();
				if (evtEnd > end)
				{
					end = evtEnd;
				}
			}
			return end;
		}
	}
}
