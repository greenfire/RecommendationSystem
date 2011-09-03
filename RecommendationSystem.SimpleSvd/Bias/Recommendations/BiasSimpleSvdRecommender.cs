using System;
using System.Collections.Generic;
using RecommendationSystem.Entities;
using RecommendationSystem.Recommendations;
using RecommendationSystem.SimpleSvd.Bias.Prediction;
using RecommendationSystem.SimpleSvd.Recommendation;
using RecommendationSystem.Svd.Foundation.Bias.Models;
using RecommendationSystem.Svd.Foundation.Prediction;

namespace RecommendationSystem.SimpleSvd.Bias.Recommendations
{
    public class BiasSimpleSvdRecommender : SimpleSvdRecommenderBase<IBiasSvdModel>
    {
        public BiasSimpleSvdRecommender(bool useBiasBins = false)
            : this(new BiasSimpleSvdPredictor(), useBiasBins)
        {}

        public BiasSimpleSvdRecommender(ISvdPredictor<IBiasSvdModel> predictor, bool useBiasBins = false)
            : base(predictor, useBiasBins)
        {}

        public override float PredictRatingForArtist(IUser user, IBiasSvdModel model, List<IArtist> artists, int artist)
        {
            return Predictor.PredictRatingForArtist(user, model, artists, artist, UseBiasBins);
        }

        public override IEnumerable<IRecommendation> GenerateRecommendations(IUser user, IBiasSvdModel model, List<IArtist> artists)
        {
            throw new NotImplementedException();
        }
    }
}