using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RecommendationSystem.Entities;
using RecommendationSystem.Svd.Foundation.Models;
using RecommendationSystem.Svd.Foundation.Recommendations;
using RecommendationSystem.Svd.Foundation.Training;

namespace RecommendationSystem.QualityTesting.Testers
{
    public class SvdTester<TSvdModel> : TesterBase
        where TSvdModel : ISvdModel
    {
        public List<IUser> TestUsers { get; set; }
        public List<IRating> TestRatings { get; set; }
        public List<IArtist> Artists { get; set; }

        public IRecommendationSystem<TSvdModel, IUser, ISvdTrainer<TSvdModel>, ISvdRecommender<TSvdModel>> RecommendationSystem { get; set; }
        public TSvdModel Model { get; set; }

        public SvdTester(string testName, IRecommendationSystem<TSvdModel, IUser, ISvdTrainer<TSvdModel>, ISvdRecommender<TSvdModel>> recommendationSystem, TSvdModel model, List<IUser> testUsers, List<IRating> testRatings, List<IArtist> artists)
        {
            RecommendationSystem = recommendationSystem;
            Model = model;
            TestUsers = testUsers;
            TestRatings = testRatings;
            Artists = artists;

            TestName = testName;
        }

        public override void Test()
        {
            base.Test();

            Timer.Restart();
            MaeAndBias[] rbsByRatings;
            var rb = TestRecommendationSystem(out rbsByRatings);
            Timer.Stop();
            for (var i = 0; i < rbsByRatings.Length; i++)
                Write(string.Format(CultureInfo.InvariantCulture, "{0}\t->\tRating:{1}\t{2}.", TestName, i + 1, rbsByRatings[i]));

            Write(string.Format(CultureInfo.InvariantCulture, "{0}\t->\tAll ratings\t{1}\t({2}).", TestName, rb, TimeSpan.FromMilliseconds(Timer.ElapsedMilliseconds)));
            FileWriter.Close();
        }

        #region CompleteTestRecommendationSystem
        private MaeAndBias TestRecommendationSystem(out MaeAndBias[] rvsByRatings)
        {
            var maeBC = new BlockingCollection<float>[5];
            for (var i = 0; i < maeBC.Length; i++)
                maeBC[i] = new BlockingCollection<float>();

            var biasBC = new BlockingCollection<float>[5];
            for (var i = 0; i < biasBC.Length; i++)
                biasBC[i] = new BlockingCollection<float>();

            var percent = TestUsers.Count / 100;
            for (var i = 0; i < TestUsers.Count; i++)
            {
                var user = TestUsers[i];
                lock (user)
                {
                    if (user.Ratings.Count > 1)
                    {
                        var originalRatings = user.Ratings;
                        foreach (var rating in user.Ratings)
                        {
                            user.Ratings = originalRatings.Where(r => r != rating).ToList();
                            var predictedRating = RecommendationSystem.Recommender.PredictRatingForArtist(user, Model, Artists, rating.ArtistIndex);
                            
                            var error = predictedRating - rating.Value;
                            biasBC[(int)rating.Value - 1].Add(error);
                            maeBC[(int)rating.Value - 1].Add(Math.Abs(error));
                            
                            Write(string.Format("{0}\t{1}", predictedRating, rating.Value), false);
                        }
                        user.Ratings = originalRatings;
                    }
                }

                if (i % percent == 0)
                    Write(string.Format("{0} {1}% with {2}", TestName, i / percent, GetMaeAndBias(biasBC, maeBC)), toFile: false);
            }

            return GetMaeAndBias(out rvsByRatings, biasBC, maeBC);
        }

        private static MaeAndBias GetMaeAndBias(BlockingCollection<float>[] biasBC, BlockingCollection<float>[] maeBC)
        {
            var totalMae = new List<float>();
            var totalBias = new List<float>();
            for (var i = 0; i < maeBC.Length; i++)
            {
                totalMae.AddRange(maeBC[i].ToList());
                totalBias.AddRange(biasBC[i].ToList());
            }

            return new MaeAndBias(totalMae, totalBias);
        }

        private static MaeAndBias GetMaeAndBias(out MaeAndBias[] rbsByRatings, BlockingCollection<float>[] biasBC, BlockingCollection<float>[] maeBC)
        {
            rbsByRatings = new MaeAndBias[5];
            var totalMae = new List<float>();
            var totalBias = new List<float>();
            for (var i = 0; i < maeBC.Length; i++)
            {
                if (maeBC[i].Count > 0)
                    rbsByRatings[i] = new MaeAndBias(maeBC[i].ToList(), biasBC[i].ToList());
                else
                    rbsByRatings[i] = new MaeAndBias();

                totalMae.AddRange(maeBC[i].ToList());
                totalBias.AddRange(biasBC[i].ToList());
            }

            return new MaeAndBias(totalMae, totalBias);
        }
        #endregion
    }
}