using System;
using System.Collections.Generic;
using System.Linq;
using GestureRecognizer.Types;

namespace GestureRecognizer.GeometricRecognizer
{
    // MAIN FUNCTIONALITY
    public partial class GeometricRecognizer
    {
        protected double HalfDiagonal, AngleRange, AnglePrecision, GoldenRatio;
        protected int NumPointsInGesture, SquareSize;
        private bool _shouldIgnoreRotation;

        protected bool RotationInvariance
        {
            get => _shouldIgnoreRotation;
            set
            {
                _shouldIgnoreRotation = value;
                AngleRange = _shouldIgnoreRotation ? 45d : 15d;
            }
        }

        protected List<GestureTemplate> Templates;

        public GeometricRecognizer()
        {
            Templates = new List<GestureTemplate>();
            LoadTemplates();
            //--- How many templates do we have to compare the user's gesture against?
            //--- Can get ~97% accuracy with just one template per symbol to recognize
            //numTemplates = 16;
            //--- How many points do we use to represent a gesture
            //--- Best results between 32-256
            NumPointsInGesture = 128;
            //--- Before matching, we stretch the symbol across a square
            //--- That way we don't have to worry about the symbol the user drew
            //---  being smaller or larger than the one in the template
            SquareSize = 250;
            //--- 1/2 max distance across a square, which is the maximum distance
            //---  a point can be from the center of the gesture
            HalfDiagonal = 0.5 * Math.Sqrt((250.0 * 250.0) + (250.0 * 250.0));
            //--- Before matching, we rotate the symbol the user drew so that the 
            //---  start point is at degree 0 (right side of symbol). That's how 
            //---  the templates are rotated so it makes matching easier
            //--- Note: this assumes we want symbols to be rotation-invariant, 
            //---  which we might not want. Using this, we can't tell the difference
            //---  between squares and diamonds (which is just a rotated square)
            RotationInvariance = false;
            AnglePrecision = 2.0;
            //--- A magic number used in pre-processing the symbols
            GoldenRatio = 0.5 * (-1.0 + Math.Sqrt(5.0));
        }

        public RecognitionResult Recognize(Path2D points)
        {
            //--- Make sure we have some templates to compare this to
            //---  or else recognition will be impossible
            if (!Templates.Any())
            {
                Console.WriteLine("No templates loaded so no symbols to match.");
                return new RecognitionResult("Unknown", 1);
            }

            points = NormalizePath(points);

            //--- Initialize best distance to the largest possible number
            //--- That way everything will be better than that
            double bestDistance = double.MaxValue;
            //--- We haven't found a good match yet
            int indexOfBestMatch = -1;

            //--- Check the shape passed in against every shape in our database
            for (int i = 0; i < Templates.Count; i++)
            {
                //--- Calculate the total distance of each point in the passed in
                //---  shape against the corresponding point in the template
                //--- We'll rotate the shape a few degrees in each direction to
                //---  see if that produces a better match
                double distance = DistanceAtBestAngle(points, Templates[i]);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    indexOfBestMatch = i;
                }
            }

            //--- Turn the distance into a percentage by dividing it by 
            //---  half the maximum possible distance (across the diagonal 
            //---  of the square we scaled everything too)
            //--- Distance = hwo different they are
            //--- Subtract that from 1 (100%) to get the similarity
            double score = 1.0 - (bestDistance / HalfDiagonal);

            //--- Make sure we actually found a good match
            //--- Sometimes we don't, like when the user doesn't draw enough points
            if (-1 == indexOfBestMatch)
            {
                Console.WriteLine("Couldn't find a good match.");
                return new RecognitionResult("Unknown", 1);
            }

            RecognitionResult bestMatch = new RecognitionResult(Templates[indexOfBestMatch].Name, score);
            return bestMatch;
        }

        private void LoadTemplates()
        {
            var samples = new SampleGestures();
            foreach (var gestureMethod in typeof(SampleGestures).GetMethods().Where(x =>
                x.Name != "GetType" && 
                x.Name != "ToString" && 
                x.Name != "Equals" && 
                x.Name != "GetHashCode"))
                Templates.Add(new GestureTemplate(gestureMethod.Name, (Path2D) gestureMethod.Invoke(samples, null)));
        }
    }
}