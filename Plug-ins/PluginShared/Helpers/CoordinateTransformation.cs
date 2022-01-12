
using System.Collections.Generic;
using System.Linq;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

namespace PluginShared.Helpers;

public static class CoordinateTransformation
{
	public static double[] TransFormCoordinate(int fromId, int toId, double[] points)
	{
		var csFact = new CoordinateSystemFactory();
		var ctFact = new CoordinateTransformationFactory();

		if (!SupportedCoordinateSystems.ContainsKey(fromId) || !SupportedCoordinateSystems.ContainsKey(toId))
		{
			return points;
		}

		var utm = ProjectedCoordinateSystem.WGS84_UTM(31, true);

		var fromSystemTrans = csFact.CreateFromWkt(SupportedCoordinateSystems[fromId]);
		var transPart1      = ctFact.CreateFromCoordinateSystems(fromSystemTrans, utm);
		var intermittent    = transPart1.MathTransform.Transform(points).ToArray();

		if (toId == fromId)
		{
			return intermittent;
		}
			
		var toSystemTrans = csFact.CreateFromWkt(SupportedCoordinateSystems[toId]);
		var transPart2    = ctFact.CreateFromCoordinateSystems(utm, toSystemTrans);
		var result        = transPart2.MathTransform.Transform(intermittent).ToArray();
			
		return result;
	}

	private static readonly Dictionary<int, string> SupportedCoordinateSystems = new()
	{
		{ 4154, "GEOGCS[\"ED50(ED77)\",DATUM[\"European_Datum_1950_1977\",SPHEROID[\"International 1924\",6378388,297,AUTHORITY[\"EPSG\",\"7022\"]],TOWGS84[-117,-132,-164,0,0,0,0],AUTHORITY[\"EPSG\",\"6154\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4154\"]]" },
		{ 4230, "GEOGCS[\"ED50\",DATUM[\"European_Datum_1950\",SPHEROID[\"International 1924\",6378388,297,AUTHORITY[\"EPSG\",\"7022\"]],TOWGS84[-87,-98,-121,0,0,0,0],AUTHORITY[\"EPSG\",\"6230\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4230\"]]" },
		{ 4258, "GEOGCS[\"ETRS89\",DATUM[\"European_Terrestrial_Reference_System_1989\",SPHEROID[\"GRS 1980\",6378137,298.257222101,AUTHORITY[\"EPSG\",\"7019\"]],TOWGS84[0,0,0,0,0,0,0],AUTHORITY[\"EPSG\",\"6258\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4258\"]]" },
		{ 4326, "GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\",SPHEROID[\"WGS 84\",6378137,298.257223563,AUTHORITY[\"EPSG\",\"7030\"]],AUTHORITY[\"EPSG\",\"6326\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4326\"]]" },
		{ 4668, "GEOGCS[\"ED79\",DATUM[\"European_Datum_1979\",SPHEROID[\"International 1924\",6378388,297,AUTHORITY[\"EPSG\",\"7022\"]],TOWGS84[-86,-98,-119,0,0,0,0],AUTHORITY[\"EPSG\",\"6668\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4668\"]]" },
		{ 28991, "PROJCS[\"Amersfoort / RD Old\",GEOGCS[\"Amersfoort\",DATUM[\"Amersfoort\",SPHEROID[\"Bessel 1841\",6377397.155,299.1528128,AUTHORITY[\"EPSG\",\"7004\"]],TOWGS84[565.2369,50.0087,465.658,-0.406857,0.350733,-1.87035,4.0812],AUTHORITY[\"EPSG\",\"6289\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4289\"]],PROJECTION[\"Oblique_Stereographic\"],PARAMETER[\"latitude_of_origin\",52.15616055555555],PARAMETER[\"central_meridian\",5.38763888888889],PARAMETER[\"scale_factor\",0.9999079],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],AXIS[\"X\",EAST],AXIS[\"Y\",NORTH],AUTHORITY[\"EPSG\",\"28991\"]]" },
		{ 28992, "PROJCS[\"Amersfoort / RD New\",GEOGCS[\"Amersfoort\",DATUM[\"Amersfoort\",SPHEROID[\"Bessel 1841\",6377397.155,299.1528128,AUTHORITY[\"EPSG\",\"7004\"]],TOWGS84[565.2369,50.0087,465.658,-0.406857,0.350733,-1.87035,4.0812],AUTHORITY[\"EPSG\",\"6289\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4289\"]],PROJECTION[\"Oblique_Stereographic\"],PARAMETER[\"latitude_of_origin\",52.15616055555555],PARAMETER[\"central_meridian\",5.38763888888889],PARAMETER[\"scale_factor\",0.9999079],PARAMETER[\"false_easting\",155000],PARAMETER[\"false_northing\",463000],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],AXIS[\"X\",EAST],AXIS[\"Y\",NORTH],AUTHORITY[\"EPSG\",\"28992\"]]" },
		{ 25831, "GEOGCS[\"ETRS89\",DATUM[\"European_Terrestrial_Reference_System_1989\",SPHEROID[\"GRS 1980\",6378137,298.257222101,AUTHORITY[\"EPSG\",\"7019\"]],TOWGS84[0,0,0,0,0,0,0],AUTHORITY[\"EPSG\",\"6258\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.01745329251994328,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4258\"]],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],PROJECTION[\"Transverse_Mercator\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",3],PARAMETER[\"scale_factor\",0.9996],PARAMETER[\"false_easting\",500000],PARAMETER[\"false_northing\",0],AUTHORITY[\"EPSG\",\"25831\"],AXIS[\"Easting\",EAST],AXIS[\"Northing\",NORTH]]" }
	};

}