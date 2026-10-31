using NUnit.Framework;
using UnityEngine;

namespace TrackingNIU.Tests
{
    public class ArtifactCollectionTests
    {
        bool _hadSaved;
        int _saved;

        // 測試會改到編輯器的 PlayerPrefs，結束後還原原本的圖鑑紀錄。
        [SetUp]
        public void SetUp()
        {
            _hadSaved = PlayerPrefs.HasKey(ArtifactCollection.PrefsKey);
            _saved = PlayerPrefs.GetInt(ArtifactCollection.PrefsKey, 0);
            ArtifactCollection.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            if (_hadSaved)
                PlayerPrefs.SetInt(ArtifactCollection.PrefsKey, _saved);
            else
                PlayerPrefs.DeleteKey(ArtifactCollection.PrefsKey);
            PlayerPrefs.Save();
        }

        [Test]
        public void All_ContainsSixArtifactsWithoutNone()
        {
            Assert.AreEqual(6, ArtifactCollection.All.Length);
            CollectionAssert.AllItemsAreUnique(ArtifactCollection.All);
            CollectionAssert.DoesNotContain(ArtifactCollection.All, ArtifactId.None);
        }

        [Test]
        public void Add_KeepsArtifactsFromEarlierRuns()
        {
            ArtifactCollection.Add(new[] { ArtifactId.GoldFoil, ArtifactId.GoldenCarp });
            ArtifactCollection.Add(new[] { ArtifactId.AgateBead, ArtifactId.GoldFoil, ArtifactId.None });

            Assert.AreEqual(3, ArtifactCollection.Count);
            Assert.IsTrue(ArtifactCollection.Has(ArtifactId.GoldFoil));
            Assert.IsTrue(ArtifactCollection.Has(ArtifactId.GoldenCarp));
            Assert.IsTrue(ArtifactCollection.Has(ArtifactId.AgateBead));
            Assert.IsFalse(ArtifactCollection.Has(ArtifactId.PotteryJar));
            Assert.IsFalse(ArtifactCollection.Has(ArtifactId.None));
        }

        [Test]
        public void Clear_RemovesEverything()
        {
            ArtifactCollection.Add(ArtifactCollection.All);
            Assert.AreEqual(6, ArtifactCollection.Count);

            ArtifactCollection.Clear();
            Assert.AreEqual(0, ArtifactCollection.Count);
        }
    }
}
