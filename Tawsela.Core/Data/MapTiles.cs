namespace Tawsela.Data
{
    public static class MapTiles
    {
        // OSM أساسي، CartoDB احتياطي، OpenTopoMap احتياط ثانوي
        public static string TileLayerJs(string mapVar = "map", string onAllTilesFailed = "")
        {
            var failed = string.IsNullOrEmpty(onAllTilesFailed) ? "" : onAllTilesFailed + ";";
            return
                "var _t0=L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png',{maxZoom:19,attribution:'© OpenStreetMap'});" +
                "var _t1=L.tileLayer('https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}.png',{maxZoom:19,attribution:'© CARTO/OSM'});" +
                "var _t2=L.tileLayer('https://{s}.tile.opentopomap.org/{z}/{x}/{y}.png',{maxZoom:19,attribution:'© OpenTopoMap/OSM'});" +
                "var _e0=0,_e1=0,_e2=0;" +
                "_t0.on('tileerror',function(){_e0++;if(_e0>=3){_e0=0;" + mapVar + ".removeLayer(_t0);" + mapVar + ".addLayer(_t1);}});" +
                "_t1.on('tileerror',function(){_e1++;if(_e1>=3){_e1=0;" + mapVar + ".removeLayer(_t1);" + mapVar + ".addLayer(_t2);}});" +
                "_t2.on('tileerror',function(){_e2++;if(_e2>=3){" + failed + "}});" +
                "_t0.addTo(" + mapVar + ");";
        }
    }
}
