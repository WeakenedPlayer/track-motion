using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

// Vegas information: https://www.vegascreativesoftware.info/us/forum/vegas-pro-scripting-faqs-resources--104563/
using ScriptPortal.Vegas;


public class EntryPoint
{
	public void FromVegas(Vegas vegas)
	{
		Track track;
		track = FindSelectedTrack(vegas.Project);

		// トラックが選択されていないか、ビデオトラックではない
		if (track == null || !track.IsVideo()) {
			MessageBox.Show("ビデオトラックを選択してください。");
			return;
		}

		// 座標データを含むファイルを選択する。
		OpenFileDialog ofd = new OpenFileDialog();
		ofd.CheckFileExists = true;
		ofd.CheckPathExists = true;
		if (ofd.ShowDialog() != DialogResult.OK)
		{
			MessageBox.Show("読み込みを中止します。");
			return;
		}

		// 既存のトラックモーションを削除する
		VideoTrack video = (VideoTrack)track;
		video.TrackMotion.MotionKeyframes.Clear();

		// [ long tick, float x, float y ] のデータを取得する。
		BinaryReader reader = new BinaryReader(File.OpenRead(ofd.FileName));
		const long dataPerRow = sizeof(long) + sizeof(float) + sizeof(float);
		long recordCount = reader.BaseStream.Length / dataPerRow;
		long skip = 0;

		// 決め打ちデータ間引き・座標算出
		// 面倒なので直打ち
		for (long i = 0; i < recordCount; i++)
		{
			if (skip > 2)
			{
				long ms = reader.ReadInt64() / 10000; // 1tick = 100ns, 
				double nx = 1920 * (reader.ReadSingle() - 0.5f);
				double ny = 1080 * (0.5f - reader.ReadSingle());

				// キーフレームを追加し、座標をセット…点数が多いとクラッシュする模様。
				TrackMotionKeyframe frame = video.TrackMotion.InsertMotionKeyframe(new Timecode(ms));
				frame.PositionX = nx;
				frame.PositionY = ny;

				skip = 0;
			}
			skip++;
		}

		reader.Close();
	}

	// 選択しているTrackを探す
	Track FindSelectedTrack(Project project)
	{
		foreach (Track track in project.Tracks)
		{
			if (track.Selected)
			{
				return track;
			}
		}
		return null;
	}
}
