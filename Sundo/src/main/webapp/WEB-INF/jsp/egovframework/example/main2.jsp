<%@ page language="java" contentType="text/html; charset=UTF-8"
    pageEncoding="UTF-8"%>
<%@ taglib prefix="c" uri="http://java.sun.com/jsp/jstl/core" %>    
<!DOCTYPE html>
<html>
<head>
<title>WMTS Layer from Capabilities</title>
<script src="https://openlayers.org/en/v5.3.0/build/ol.js"></script>
<link rel="stylesheet"    href="https://openlayers.org/en/v5.3.0/css/ol.css" type="text/css">

<!--
https://openlayers.org/en/v6.4.3/build/ol.js
https://openlayers.org/en/v5.3.0/build/ol.js
https://openlayers.org/en/v4.6.5/build/ol.js
https://openlayers.org/en/v3.20.1/build/ol.js

https://openlayers.org/en/v6.4.3/css/ol.css
https://openlayers.org/en/v5.3.0/css/ol.css
https://openlayers.org/en/v4.6.5/css/ol.css
https://openlayers.org/en/v3.20.1/css/ol.css
-->


<script src="https://code.jquery.com/jquery-2.2.3.min.js"></script>
<script src="https://kit.fontawesome.com/63ed9e5411.js" crossorigin="anonymous"></script>
<link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.1/dist/css/bootstrap.min.css" rel="stylesheet" integrity="sha384-4bw+/aepP/YC94hEpVNVgiZdgIC5+VKNBQNGCHeKRQN+PtmoHDEXuppvnDJzQIu9" crossorigin="anonymous">
<!-- <script src="/js/jquery.xdomainajax.js"></script> -->
<style type="text/css">

/* 화면 너비가 768px 이하일 때 스타일 */
@media (max-width: 768px) {
    #mapContainer {
        display: block; /* 블록 레이아웃으로 변경 */
        width: 100%; /* 전체 너비로 확장 */
    }
    
    #sideTab {
        width: 100%; /* 전체 너비로 확장 */
        float: none; /* float 제거 */
    }
}

#box3{
   display:none;
}

.ol-mycontrol {
    background-color: rgba(255, 255, 255, 0.4);
    border-radius: 4px;
    padding: 4px;
    margin-left: 10px;
    position: block;
    width:136px;
    top: 5px;
    left:40px;
    width: fit-content;
}

.ol-mycontrol button {
    float:left;
}
.ol-mycontrol button.on {
    background-color:rgba(124,60,55,.5);
}

#mapContainer{
    display: flex;
    min-width: 768px;
}

#sideTab{
    
  
      background-color: rgb(230, 229, 229);
    width: 470px;
    height: 850px;
    float: left;
    text-align: center;
}

#logoImg{
   height: 50px;
    width: 50px;
}

.roundbox{
    
    margin: 0 auto;
    width: 300px;
    background-color: white;
    border-radius: 20px;
    margin-top: 40px;
            
        }
        
 #box1{
    height: 100px;
    display: flex;
    justify-content: center;
    align-items: center;
}

#carlist{
    padding-top: 20px;
    padding-bottom: 20px;
}

#selectcar{
   padding-top: 20px;
    padding-bottom: 20px;
}

#mapContainer{
   width:80%;
   margin:0 auto;
   
}

#toptext{
   padding-top:30px;
}

.carbtnList{
   border: none;
    background-color: transparent;
}

#close{
   border: none;
    background-color: transparent;
    
}

#hybrid.on{
    
    background-color: rgb(0, 0, 255);
    color: white;
      
}
</style>

</head>
<body>

   <input type="text" id="car">   
   
    <div id="mapContainer">
        
        <!-- 사이드메뉴 div -->
        <div id="sideTab">
             <div id='toptext'>
               <p><img id="logoImg" src="<c:url value='/images/egovframework/example/yongin.svg'/>" alt=""/><spring:message code="list.sample" /><b>용인시 청소차 관제 시스템</b></p>
            </div> 
      <hr>
      
      <!-- 버튼목록 -->
      <div class='roundbox' id='box1'>
                <i class="fa-solid fa-map-location-dot fa-xl" style='padding-right: 10px;'></i>
                <div class="btn-group" role="group" aria-label="Basic outlined example">
                    <button type="button" class="btn btn-outline-primary active" id='normal'>기본</button>
                    <button type="button" class="btn btn-outline-primary" id='wisung'>위성</button>
                    <button type="button" class="btn btn-outline-primary on" id='hybrid'>하이브리드</button>
                </div>
            </div> 
            
       <!-- 차량 리스트 -->            
         <div class='roundbox' id='box2'>
                <div id='carlist'>
                    <h5><b>차량 목록</b></h5> 
                    <hr>
                    <div id='selectcarlist'>
                       
                       <!-- 차량 반복 출력  -->
                       <c:forEach items="${pList}" var="point">
                     <p><i class="fa-solid fa-truck"></i> <button value='${point.car_num}' class='carbtnList' onclick='selectcar(${point.car_num})'>${point.car_num}</button></p> <br>
                  </c:forEach>
                        
                        
                    </div>
                </div>                       
            </div>              
            
            
          <!-- 선택 차량 -->
        <div class='roundbox' id='box3'>
                <div id='selectcar'>
                    <h5><b id="carName"></b></h5> 
                    <hr>
                    <p>
                        <span id='datetext'>날짜선택 :</span>
                        <br>
                        <form>
                            <input type="date" id='currentDate'>
                            <input type="button" value="확인" class='btn btn-outline-primary' onclick="addCleanLayer()">
                        </form>
                    </p>
                    <hr>
                    <div id='resbox'>
                        <p>운행 시간 : <b>변수줄거야</b></p>
                        <p>청소 비율 : <b>변수줄거야</b></p>
                    </div>
                    <hr>
                    <button id='close'>접기</button>
                </div>
            </div>
              
            
        </div>
        
        
        
        
        <div id="maps" style="width: 100%; height: 850px; left: 0px; top: 0px"></div>
    </div>
   
   <script>
   
      
      
      window.addEventListener("load", function() {
        
         
         
         // Call the button click event handler function
         base_button.onclick();
         hybrid.onclick();

         var box3 = document.getElementById('box3');
         var close_button = document.getElementById('close'); // Declare the close button as a variable
         var selectcar_buttons = document.querySelectorAll('.carbtnList');

         close_button.onclick = function() {
           // hide the box3 element
           box3.style.display = "none";
         };

         // Loop through each selectcar_button and attach the click event handler
         selectcar_buttons.forEach(function(button) {
           button.onclick = function() {
             box3.style.display = "block";
            
                document.querySelector('#car').value = this.value;
                
                var carName = document.querySelector('#carName');
                carName.innerHTML = this.value;
             
           };
         });
         
          
         
         
       });

   
   /*
      http://openlayers.org/en/latest/examples/wmts-layer-from-capabilities.html?q=WMTSCapabilities
   */
   
   var VworldBase,VworldSatellite,VworldGray,VworldMidnight,VworldHybrid;
   var attr = '&copy; <a href="http://dev.vworld.kr">vworld</a>';
   var VworldHybrid = new ol.source.XYZ({
      url: 'https://api.vworld.kr/req/wmts/1.0.0/CEB52025-E065-364C-9DBA-44880E3B02B8/Hybrid/{z}/{y}/{x}.png'
   }); //문자 타일 레이어
   
   var VworldSatellite = new ol.source.XYZ({
      url: 'https://api.vworld.kr/req/wmts/1.0.0/CEB52025-E065-364C-9DBA-44880E3B02B8/Satellite/{z}/{y}/{x}.jpeg'
      ,attributions : attr
   }); //항공사진 레이어 타일

   var VworldBase = new ol.source.XYZ({
      url: 'https://api.vworld.kr/req/wmts/1.0.0/CEB52025-E065-364C-9DBA-44880E3B02B8/Base/{z}/{y}/{x}.png'
      ,attributions : attr
   }); // 기본지도 타일

   var VworldGray =  new ol.source.XYZ({
      url: 'https://api.vworld.kr/req/wmts/1.0.0/CEB52025-E065-364C-9DBA-44880E3B02B8/gray/{z}/{y}/{x}.png'
      ,attributions : attr
   }); //회색지도 타일
   
   var VworldMidnight =  new ol.source.XYZ({
      url: 'https://api.vworld.kr/req/wmts/1.0.0/CEB52025-E065-364C-9DBA-44880E3B02B8/midnight/{z}/{y}/{x}.png'
      ,attributions : attr
   })
   
   /*
      control 설정
   */
   
   /* sideTab 버튼 변수 설정 */
   var hybrid = document.querySelector('#hybrid');
   var wisung = document.querySelector('#wisung');
   var normal = document.querySelector('#normal');

   // 하이브리드 지도설정
   hybrid.onclick=function(){
        
      var _this = this;
       maps.getLayers().forEach(function (layer) {
           if (layer.get("name") == "hybrid") {
               if (_this.classList.contains("on")) {
                   layer.setSource(null);
                   _this.classList.remove("on");
               } else {
                   _this.classList.add("on");
                   layer.setSource(VworldHybrid);
               }
           }
       })
   }

   // 위성지도
   wisung.onclick=function(){
        maps.getLayers().forEach(function(layer){
            if(layer.get("name")=="vworld"){
                layer.setSource(VworldSatellite)
            }
        })
        
        normal.classList.remove("active");
        wisung.classList.add("active");
   }

   normal.onclick=function(){
        maps.getLayers().forEach(function(layer){
            if(layer.get("name")=="vworld"){
                layer.setSource(VworldBase)
            }
        })
        
        normal.classList.add("active");
        wisung.classList.remove("active");
   }
   
   
   
   var base_button = document.createElement('button');
   base_button.innerHTML = 'B';
   var gray_button = document.createElement('button');
   gray_button.innerHTML = 'G';
   var midnight_button = document.createElement('button');
   midnight_button.innerHTML = 'M';
   var hybrid_button = document.createElement('button');
   hybrid_button.innerHTML = 'H';
   hybrid_button.className='on';
   var sate_button = document.createElement('button');
   sate_button.innerHTML = 'S';
    var element = document.createElement('div');
    element.className = 'rotate-north ol-unselectable ol-control ol-mycontrol';
    
    base_button.onclick=function(){
        maps.getLayers().forEach(function(layer){
         if(layer.get("name")=="vworld"){
            layer.setSource(VworldBase)
         }
       })
       
      
    }
    gray_button.onclick=function(){
        maps.getLayers().forEach(function(layer){
         if(layer.get("name")=="vworld"){
            layer.setSource(VworldGray)
         }
       })
    }
    midnight_button.onclick=function(){
        maps.getLayers().forEach(function(layer){
         if(layer.get("name")=="vworld"){
            layer.setSource(VworldMidnight)
         }
       })
    }
    sate_button.onclick=function(){
        maps.getLayers().forEach(function(layer){
         if(layer.get("name")=="vworld"){
            layer.setSource(VworldSatellite)
         }
       })
       
       
       
    }
    hybrid_button.onclick=function(){
       var _this = this;
         maps.getLayers().forEach(function(layer){
            if(layer.get("name")=="hybrid"){
               if(_this.className == "on"){
                layer.setSource(null)
                _this.className ="";
               }else{
                  _this.className ="on";
                  
                  layer.setSource(VworldHybrid)
               }
            }
          })
    }
    
    
    element.appendChild(base_button);
    element.appendChild(gray_button);
    element.appendChild(midnight_button);
    element.appendChild(sate_button);
    element.appendChild(hybrid_button);
    
    
    var layerControl = new ol.control.Control({element: element});
       
   maps = new ol.Map({
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
      target: 'maps',
      view: new ol.View({
         center: ol.proj.transform([127.1775537, 37.2410864], 'EPSG:4326', 'EPSG:900913'),
         zoom: 11,
         minZoom : 0,
         maxZoom : 21
      })
   });

   // 추가한 레이어
   var boundary = new ol.layer.Tile({
    source: new ol.source.TileWMS({
            url: 'http://localhost:8000/geoserver/opengis/wms',
            params: {
                'LAYERS': 'opengis:youngin',
                'TILED': true,
            },
            serverType: 'geoserver',
        })
     })

     var link = new ol.layer.Tile({
        source: new ol.source.TileWMS({
            //Vworld Tile 변경
            url: 'http://localhost:8000/geoserver/wms',
            params: {
            'layers' : 'geoserver:Link',
            'tiled' : 'true'
            },
            serverType: 'geoserver'
        })
     })
   
     var node = new ol.layer.Tile({
        source: new ol.source.TileWMS({
             url: 'http://localhost:8000/geoserver/opengis/wms',
             params: {
                 'LAYERS': 'opengis:select_link',
                 'TILED': true,
             },
             serverType: 'geoserver',
         })
     })
   
   
   
   /* function addCleanLayer() {
      // Store the value of the selected date in the date variable.
      var date = document.querySelector('#currentDate').value;
      var car_num = document.querySelector('#car').value;

      // Create an object to send as data in the fetch request.
      var obj = {
        'car_num': car_num,
        'date': date
      }

      var clean_o = new ol.layer.Tile({
        source: new ol.source.TileWMS({
          url: 'http://localhost:8000/geoserver/opengis/wms',
          params: {
            'layers': 'opengis:Clean_O',
            'tiled': 'true',
            'VIEWPARAMS': 'date:' + date
          },
          serverType: 'geoserver'
        })
      })

      var clean_x = new ol.layer.Tile({
        source: new ol.source.TileWMS({
          url: 'http://localhost:8000/geoserver/opengis/wms',
          params: {
            'layers': 'opengis:Clean_X',
            'tiled': 'true',
            'VIEWPARAMS': 'date:' + date
          },
          serverType: 'geoserver'
        })
      })

      fetchPost('/clean', obj)
        .then(map => {
          // Remove only the clean_o and clean_x layers
          maps.getLayers().forEach(layer => {
            if (layer === clean_o || layer === clean_x) {
              maps.removeLayer(layer);
            }
          });

          // If there is data, add new layers
          if (map.result == 'Yes Data') {
            console.log(map.vo);
            maps.addLayer(clean_o);
            maps.addLayer(clean_x);
          } else {
            console.log(map.result);
            var resbox = document.querySelector('#resbox');
            const text = 'No data.'
            resbox.innerHTML = text;
          }
        
        })
    } */
    
    function addCleanLayer() {
        // Store the value of the selected date in the date variable.
        var date = document.querySelector('#currentDate').value;
        var car_num = document.querySelector('#car').value;
         
        // 레이어 삭제
        maps.getLayers().getArray()
        .filter(layer => layer.get('name') === 'clean_x' || layer.get('name') === 'clean_o')
        .forEach(layer => maps.removeLayer(layer));
        
        // Create an object to send as data in the fetch request.
        var obj = {
          'car_num': car_num,
          'date': date
        }

        var clean_o = new ol.layer.Tile({
          name : 'clean_o',
          source: new ol.source.TileWMS({
            url: 'http://localhost:8000/geoserver/opengis/wms',
            params: {
              'layers': 'opengis:Clean_O',
              'tiled': 'true',
              'VIEWPARAMS': 'date:' + date
            },
            serverType: 'geoserver'
          })
           
        })

        var clean_x = new ol.layer.Tile({
         name : 'clean_x',
         source: new ol.source.TileWMS({
            url: 'http://localhost:8000/geoserver/opengis/wms',
            params: {
              'layers': 'opengis:Clean_X',
              'tiled': 'true',
              'VIEWPARAMS': 'date:' + date
            },
            serverType: 'geoserver',
            
          })
             
        })
       

        fetchPost('/clean', obj)
        
          .then(map => {
            // If there is data, add new layers
            if (map.result == 'Yes Data') {
              console.log(map.vo);
              maps.addLayer(clean_o);
              maps.addLayer(clean_x);
           
            } else {
                
            maps.getLayers().getArray()
           .filter(layer => layer.get('name') === 'clean_x' || layer.get('name') === 'clean_o')
           .forEach(layer => maps.removeLayer(layer));               
             
            console.log(map.result);
              var resbox = document.querySelector('#resbox');
              const text = 'No data.'
              resbox.innerHTML = text;
              
            }
          })
      }
   

     maps.addLayer(boundary);
     maps.addLayer(link);
     maps.addLayer(node);
     
     
     
     /*====================================================================*/
     
     // Get방식 fetch : 요청(url)과 함수(callback)를 매개변수로 함
   function fetchGet(url, callback){
      try{
      fetch(url)
         .then(response => response.json())
         .then(map => callback(map));
      } catch(e){
         console.log('fetchGet', e);
      }
   }

   function fetchPost(url, obj) {
        return fetch(url, {
          method: 'post',
          headers: {
            'Content-Type': 'application/json'
          },
          body: JSON.stringify(obj)
        })
          .then(response => response.json());
      }
   
   // Post방식 fetch : 요청(url)과  객체(obj) 그리고 함수(callback)를 매개변수로 함
   /* function fetchPost(url, obj, callback){
      try{
         fetch(url
               , {method : 'post' 
                  , headers : {'Content-Type' : 'application/json'}
                  , body : JSON.stringify(obj)})
            .then(response => response.json())
            .then(map => callback(map));
      } catch(e){
         console.log('fetchPost', e);
      }
   } */
    </script>
</body>
</html>