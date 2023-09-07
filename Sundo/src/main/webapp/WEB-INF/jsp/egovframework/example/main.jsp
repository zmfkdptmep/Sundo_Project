<%@ page contentType="text/html; charset=utf-8" pageEncoding="utf-8"%>
<%@ taglib prefix="c"      uri="http://java.sun.com/jsp/jstl/core" %>
<!DOCTYPE html>
<html lang="ko">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>레이아웃</title>
    <script src="https://kit.fontawesome.com/63ed9e5411.js" crossorigin="anonymous"></script>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.1/dist/css/bootstrap.min.css" rel="stylesheet" integrity="sha384-4bw+/aepP/YC94hEpVNVgiZdgIC5+VKNBQNGCHeKRQN+PtmoHDEXuppvnDJzQIu9" crossorigin="anonymous">
    <!-- jquery -->
    <script src="https://code.jquery.com/jquery-3.6.0.min.js"></script>
    
    <!-- openlayers -->
    <link rel="stylesheet" href="https://openlayers.org/en/v4.6.5/css/ol.css" type="text/css">
    
    
    <script src="https://openlayers.org/en/v4.6.5/build/ol.js"></script>
    <style>
        main{            
            margin: 0 auto;
            width: 1500px;
        }
        .map {
            width: 100%;
            height: 850px;
        }
        #left{
            background-color: rgb(230, 229, 229);
            width: 350px;
            height: 850px;
            float: left;
            text-align: center;
        }
        #right{
            width: 1100px;
            height: 850px;
            float: left;
        }
        .roundbox{
            margin: 0 auto;
            width: 300px;
            background-color: white;
            border-radius: 20px;
            
        }
        #box1{
            height: 100px;
            display : flex;
            justify-content: center;
            align-items : center;
        }
        #box3{
        	padding-top: 20px;
            padding-bottom: 20px;
        }
        #carlist{
            padding-top: 20px;
            padding-bottom: 20px;
        }
        .textbox{
            border: none;
        }
        #close{
            border:none;
            background-color: transparent;
            margin-left: 200px;            
        }
        #selectcar{
            padding-top: 20px;
            padding-bottom: 20px;
        }
        #toptext{
            padding-top: 20px;            
        }
        #resbox{
        	margin-left : 100px;
            margin-right: 100px;     
        }
        #currentDate{
            width: 190px;
            height: 35px;
        }
        #datetext{
            margin-right: 190px;   
        }
        #selectcarlist{
            margin-right: 100px;
        }        
        .carbtn{
            border: none;
            background-color: transparent;
        }
        .carbtn:hover{
            background-color: rgb(240, 240, 240);
        }
    </style>    
</head>
<body>
    <main>
    	<!--  
    	<jsp:include page="/WEB-INF/jsp/egovframework/example/calendar.jsp"/>
    	-->
        <div id='left'>
            <div id='toptext'>
                
                <p><img  src="/images/egovframework/example/yongin.svg" alt="용인시 로고" style='height: 50px; width: 50px;'> <b>용인시 청소차 관제 시스템</b></p>
            </div>            
            <hr>
            <br>
            <div class='roundbox' id='box1'>
                <i class="fa-solid fa-map-location-dot fa-xl" style='padding-right: 10px;'></i>
                <div class="btn-group" role="group" aria-label="Basic outlined example">
                    <button type="button" class="btn btn-outline-primary" id='base_button'>기본</button>
                    <button type="button" class="btn btn-outline-primary" id='sate_button'>위성</button>
                    <button type="button" class="btn btn-outline-primary" id='hybrid_button'>하이브리드</button>
                </div>
            </div>
            <br>
            <script>
            	window.addEventListener('load', function(){
            		
            		var carList = JSON.parse('${carList}');
	            	var selectCar = document.querySelector('#selectcarlist');
	            	selectCar.innerHTML = '';
	            	console.log('carList : ',carList);
	            	
	            	carList.forEach(car=>{
	            		
	            		selectCar.innerHTML += "<p><i class='fa-solid fa-truck'></i> <button value="+car.car_num+" class='carbtn' onclick='selectcar(this.value)'>"+car.car_num+"</button></p>"
	            	});
	            	
	            
            	});
            	
            	function selectcar(car_num){
            		
            		carTitle.innerHTML = '<b>'+car_num+'</b>';
            		carTitle.value = car_num;
            		car_name = car_num;
            		
            	}
            </script>
            <div class='roundbox' id='box2'>
                <div id='carlist'>
                    <h5><b>차량 목록</b></h5> 
                    <hr>
                    <div id='selectcarlist'>
                    </div>
                </div>                       
            </div>
            <br>
            <div class='roundbox' id='box3'>
                <div id='selectCarView'>
                    <h5 id="carTitle" value=""><b>차량을 선택해 주세요</b></h5> 
                    <hr>
                    <p>
                        <span id='datetext'>날짜선택 :</span>
                        <br>
                        <form>
    <input type="date" id='currentDate'>
    <input id="dateSubmit" type="submit" value="확인" class='btn btn-outline-primary'>
</form>

<script>

	window.addEventListener('load', function(){
		
		
		var submitBtn = document.querySelector('#dateSubmit');

	    submitBtn.addEventListener('click', function(e){
	        e.preventDefault();
	        
	        var nowDate = document.querySelector('#currentDate').value;

	        const url = '/date';
	        const car_name = document.querySelector('#carTitle').value;
	        const data = {car_num: car_name, date : nowDate};
			
	        console.log('car_name : ',car_name);
	        
	        fetch(url, {
	          method: 'POST',
	          headers: {
	            'Content-Type': 'application/json'
	          },
	          body: JSON.stringify(data),
	        })
	        .then(response => response.json())
	        .then(responseData => {
	          console.log('Response from server:', responseData);
	          
	          date = responseData.date;
	          
	          if(responseData.time != null){
	        	  
	        	  console.log('responseData.lon : ', responseData.lon);
	        	  console.log('responseData.lat : ', responseData.lat);
	        	  
	        	  
		          timeValue.innerHTML = '운행 시간  <br><b>'+responseData.time+'</b>';
		          ratioValue.innerHTML = '청소 비율  <br><b>'+responseData.ratio+'%</b>';
		          var newCenter = ol.proj.transform([parseFloat(responseData.lon), parseFloat(responseData.lat)], 'EPSG:4326', 'EPSG:900913');
		          map.getView().setCenter(newCenter);
		          map.getView().setZoom(17);
		          
	          } else {
	        	  
	        	  timeValue.innerHTML = '운행 시간  <br><b>데이터 없음</b>';
		          ratioValue.innerHTML = '청소 비율  <br><b>데이터 없음</b>';
	          }
	          
	          
	          
	          
	           
	         
	          
	       
	        // 추가한 레이어
	       
        
	          var link = new ol.layer.Tile({
	        	 name : 'link',
	             source: new ol.source.TileWMS({
	                 //Vworld Tile 변경
	                 url: 'http://localhost:8080/geoserver/hi/wms',
	                 params: {
	                 'layers' : 'hi:clean_o',
	                 'tiled' : 'true',
	                 'VIEWPARAMS' : 'date:' + date + ';car_num:' + car_name
	                 
	                 
	                 },
	                 serverType: 'geoserver'
	             })
	          })
	     
	          var node = new ol.layer.Tile({
	        	  name : 'node',
	             source: new ol.source.TileWMS({
	                 //Vworld Tile 변경
	                 url: 'http://localhost:8080/geoserver/hi/wms',
	                 params: {
	                 'layers' : 'hi:clean_x',
	                 'tiled' : 'true',
	                 'VIEWPARAMS' : 'date:' + date + ';car_num:' + car_name
	                 },
	                 serverType: 'geoserver'
	             })
	          })
	          var line = new ol.layer.Tile({
	        	  name : 'node',
	             source: new ol.source.TileWMS({
	                 //Vworld Tile 변경
	                 url: 'http://localhost:8080/geoserver/hi/wms',
	                 params: {
	                 'layers' : 'hi:clean_line',
	                 'tiled' : 'true',
	                 'VIEWPARAMS' : 'date:' + date
	                 },
	                 serverType: 'geoserver'
	             })
	          })
	          var startPoint = new ol.layer.Tile({
	        	  name : 'node',
	             source: new ol.source.TileWMS({
	                 //Vworld Tile 변경
	                 url: 'http://localhost:8080/geoserver/hi/wms',
	                 params: {
	                 'layers' : 'hi:start_point',
	                 'tiled' : 'true',
	                 'VIEWPARAMS' : 'date:' + date
	                 },
	                 serverType: 'geoserver'
	             })
	          })
	          var endPoint = new ol.layer.Tile({
	        	  name : 'node',
	             source: new ol.source.TileWMS({
	                 //Vworld Tile 변경
	                 url: 'http://localhost:8080/geoserver/hi/wms',
	                 params: {
	                 'layers' : 'hi:end_point',
	                 'tiled' : 'true',
	                 'VIEWPARAMS' : 'date:' + date
	                 },
	                 serverType: 'geoserver'
	             })
	          })
	          
	       // 이전 날짜의 레이어 제거
	          map.getLayers().getArray()
              .filter(layer => layer.get('name') === 'link' || layer.get('name') === 'node' || layer.get('name') == 'line' || layer.get('name') == 'startPoint' || layer.get('name') == 'endPoint')
              .forEach(layer => map.removeLayer(layer));
	        
	          map.addLayer(line);
	          map.addLayer(link);
	          map.addLayer(node);
	          map.addLayer(startPoint);
	          map.addLayer(endPoint);
	          
	        })
	        .catch(error => {
	          console.error('Error:', error);
	        });
	        
	        
	        // 3초마다 해당 함수 호출하는 상수
	        
	        const intervalId = setInterval(Socket, 3000);
	      
		    $('#stopBtn').click(function(){
				
				clearInterval(intervalId);
				
			});
	    });	
			
	    
	    
	    
	    
	    
	
	});
	
	
	function Socket(){
		
		var nowDate = document.querySelector('#currentDate').value;

        const url = '/date';
        const car_name = document.querySelector('#carTitle').value;
        const data = {car_num: car_name, date : nowDate};
		
        console.log('car_name : ',car_name);
        
        fetch(url, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json'
          },
          body: JSON.stringify(data),
        })
        .then(response => response.json())
        .then(responseData => {
          console.log('Response from server:', responseData);
          
          date = responseData.date;
          
          if(responseData.time != null){
        	  
	          timeValue.innerHTML = '운행 시간  <br><b>'+responseData.time+'</b>';
	          ratioValue.innerHTML = '청소 비율  <br><b>'+responseData.ratio+'%</b>';
	          var newCenter = ol.proj.transform([parseFloat(responseData.lon), parseFloat(responseData.lat)], 'EPSG:4326', 'EPSG:900913');
	          map.getView().setCenter(newCenter);
	          map.getView().setZoom(19);
          } else {
        	  
        	  timeValue.innerHTML = '운행 시간  <br><b>데이터 없음</b>';
	          ratioValue.innerHTML = '청소 비율  <br><b>데이터 없음</b>';
          }
          
      
        // 추가한 레이어
       
    
          var link = new ol.layer.Tile({
        	 name : 'link',
             source: new ol.source.TileWMS({
                 //Vworld Tile 변경
                 url: 'http://localhost:8080/geoserver/hi/wms',
                 params: {
                 'layers' : 'hi:clean_o',
                 'tiled' : 'true',
                 'VIEWPARAMS' : 'date:' + date + ';car_num:' + car_name
                 
                 
                 },
                 serverType: 'geoserver'
             })
          })
     
          var node = new ol.layer.Tile({
        	  name : 'node',
             source: new ol.source.TileWMS({
                 //Vworld Tile 변경
                 url: 'http://localhost:8080/geoserver/hi/wms',
                 params: {
                 'layers' : 'hi:clean_x',
                 'tiled' : 'true',
                 'VIEWPARAMS' : 'date:' + date + ';car_num:' + car_name
                 },
                 serverType: 'geoserver'
             })
          })
          var line = new ol.layer.Tile({
        	  name : 'line',
             source: new ol.source.TileWMS({
                 //Vworld Tile 변경
                 url: 'http://localhost:8080/geoserver/hi/wms',
                 params: {
                 'layers' : 'hi:clean_line',
                 'tiled' : 'true',
                 'VIEWPARAMS' : 'date:' + date
                 },
                 serverType: 'geoserver'
             })
          })
          var startPoint = new ol.layer.Tile({
        	  name : 'startPoint',
             source: new ol.source.TileWMS({
                 //Vworld Tile 변경
                 url: 'http://localhost:8080/geoserver/hi/wms',
                 params: {
                 'layers' : 'hi:start_point',
                 'tiled' : 'true',
                 'VIEWPARAMS' : 'date:' + date
                 },
                 serverType: 'geoserver'
             })
          })
          var endPoint = new ol.layer.Tile({
        	  name : 'endPoint',
             source: new ol.source.TileWMS({
                 //Vworld Tile 변경
                 url: 'http://localhost:8080/geoserver/hi/wms',
                 params: {
                 'layers' : 'hi:end_point',
                 'tiled' : 'true',
                 'VIEWPARAMS' : 'date:' + date
                 },
                 serverType: 'geoserver'
             })
          })
          
       // 이전 날짜의 레이어 제거
          map.getLayers().getArray()
          .filter(layer => layer.get('name') === 'link' || layer.get('name') === 'node' || layer.get('name') == 'line' || layer.get('name') == 'startPoint' || layer.get('name') == 'endPoint')
          .forEach(layer => map.removeLayer(layer));
        
          map.addLayer(line);
          map.addLayer(link);
          map.addLayer(node);
          map.addLayer(startPoint);
          map.addLayer(endPoint);
          
        })
        .catch(error => {
          console.error('Error:', error);
        });

		
		
	}

</script>
                        
                        
                    </p>
                    <hr>
                    <div id='resbox'>
                        <p id="timeValue">운행 시간  <br><b>데이터 없음</b></p>
                        <p id="ratioValue">청소 비율  <br><b>데이터 없음</b></p>
                    </div>
                    
                  
                    
                    
                    <hr>
                    <button id='stopBtn'>멈춰!</button>
                    
					<div id="drop_zone">여기에 드래그 앤 드롭</div>

					<!-- 이 폼은 필요하지만 실제로는 숨겨져 있음 -->
					<form id="uploadForm" name="uploadForm" enctype="multipart/form-data" style="display:none;">
					    <input type="file" multiple name="file" id="file" />
					    <input type="button" value="업로드" id="uploadButton" />
					</form>
					
					<script>
					window.addEventListener('load', function() {
					    var dropZone = document.getElementById('drop_zone');
					
					    // Drag over and drag enter events
					    dropZone.addEventListener('dragover', function(e) {
					        e.preventDefault();
					        e.stopPropagation();
					        e.dataTransfer.dropEffect = 'copy';
					    });
					
					    dropZone.addEventListener('dragenter', function(e) {
					        e.preventDefault();
					        e.stopPropagation();
					    });
					
					    // Drop event
					    dropZone.addEventListener('drop', function(e) {
					        e.preventDefault();
					        e.stopPropagation();
					
					        var files = e.dataTransfer.files; // Array of all files
					
					        var formData = new FormData();
					
					        // Loop through each file and append it to FormData
					        for (var i = 0; i < files.length; i++) {
					            formData.append('file', files[i]);
					        }
					
					        $.ajax({
					            url: '/upload',
					            type: 'POST',
					            data: formData,
					            processData: false,
					            contentType: false
					        })
					        .done(function(result) {
					            alert(result);
					        })
					        .fail(function(error) {
					            alert("An error occurred.");
					        });
					    });
					});
					</script>
					
                    
                    
                </div>
            </div>
        </div>
        <div id='right'>
            <div id='map' class="map"></div>
        </div>
    </main>
    
    <script>
    
    	window.addEventListener('load', function(){
    		
 
    		
    		
    		/*
            http://openlayers.org/en/latest/examples/wmts-layer-from-capabilities.html?q=WMTSCapabilities
         */
         
         var VworldBase,VworldSatellite,VworldGray,VworldMidnight,VworldHybrid;
         var attr = '&copy; <a href="http://dev.vworld.kr">vworld</a>';
         var VworldHybrid = new ol.source.XYZ({
            url: 'https://api.vworld.kr/req/wmts/1.0.0/1BED7823-51FA-30E5-8664-4B59FDCC983E/Hybrid/{z}/{y}/{x}.png'
         }); //문자 타일 레이어
         
         var VworldSatellite = new ol.source.XYZ({
            url: 'https://api.vworld.kr/req/wmts/1.0.0/1BED7823-51FA-30E5-8664-4B59FDCC983E/Satellite/{z}/{y}/{x}.jpeg'
            ,attributions : attr
         }); //항공사진 레이어 타일
      
         var VworldBase = new ol.source.XYZ({
            url: 'https://api.vworld.kr/req/wmts/1.0.0/1BED7823-51FA-30E5-8664-4B59FDCC983E/Base/{z}/{y}/{x}.png'
            ,attributions : attr
         }); // 기본지도 타일      
         /*
            control 설정
         */
         
          var element = document.createElement('div');
          element.className = 'rotate-north ol-unselectable ol-control ol-mycontrol';
          
          base_button.onclick=function(){
              map.getLayers().forEach(function(layer){
               if(layer.get("name")=="vworld"){              
                 
                  layer.setSource(VworldBase)
               }
             })
          }
          sate_button.onclick=function(){
              map.getLayers().forEach(function(layer){
               if(layer.get("name")=="vworld"){
                  layer.setSource(VworldSatellite)
               }
             })
          }
          hybrid_button.onclick=function(){
             var _this = this;
               map.getLayers().forEach(function(layer){
                  if(layer.get("name")=="hybrid"){
                     if(_this.className == "on btn btn-outline-primary"){
                      layer.setSource(null)
                      _this.className ="btn btn-outline-primary";
                     }else{
                        _this.className ="on btn btn-outline-primary";
                        
                        layer.setSource(VworldHybrid)
                     }
                  }
                })
          }
          
          var layerControl = new ol.control.Control({element: element});
          var date;
          var car_name;
          
          console.log(date);
          
          
         map = new ol.Map({
            controls: ol.control.defaults().extend([
               layerControl,new ol.control.OverviewMap(),new ol.control.ZoomSlider()
            ]),
            layers: [
               
               new ol.layer.Tile({
                  source: VworldSatellite,
                  name:"vworld"
               }),new ol.layer.Tile({
                  source: VworldHybrid,
                  name:"hybrid"
               })
            ],
            target: 'map',
            view: new ol.View({
               center: ol.proj.transform([127.1775537, 37.2410864], 'EPSG:4326', 'EPSG:900913'),
               zoom: 10,
               minZoom : 0,
               maxZoom : 21
            })
         });
      
         // 추가한 레이어
         var boundary = new ol.layer.Tile({
              source: new ol.source.TileWMS({
                  //Vworld Tile 변경
                  url: 'http://localhost:8080/geoserver/hi/wms',
                  params: {
                  'layers' : 'hi:용인시',
                  'tiled' : 'true'
                  },
                  serverType: 'geoserver'
              })
           })
      	  
           
           map.addLayer(boundary);
    		
    	});
    
        
          
          </script>

    <script>document.getElementById('currentDate').value = new Date().toISOString().substring(0, 10);</script>
    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.1/dist/js/bootstrap.bundle.min.js" integrity="sha384-HwwvtgBNo3bZJJLYd8oVXjrBZt8cqVSpeBNS5n7C8IVInixGAoxmnlMuBnhbgrkm" crossorigin="anonymous"></script>
</body>
</html>
