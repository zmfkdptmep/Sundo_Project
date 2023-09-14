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

<!-- chart.js cnd -->
<script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
<!--  <script src="https://code.jquery.com/jquery-2.2.3.min.js"></script>-->
<script src="https://code.jquery.com/jquery-3.6.0.min.js"></script>
<script src="https://maxcdn.bootstrapcdn.com/bootstrap/4.5.2/js/bootstrap.min.js"></script>
<script src="https://kit.fontawesome.com/63ed9e5411.js" crossorigin="anonymous"></script>

<!-- 폰트 -->
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link href="https://fonts.googleapis.com/css2?family=Noto+Sans+KR:wght@300;400;600&display=swap" rel="stylesheet">
<link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.1/dist/css/bootstrap.min.css" rel="stylesheet" integrity="sha384-4bw+/aepP/YC94hEpVNVgiZdgIC5+VKNBQNGCHeKRQN+PtmoHDEXuppvnDJzQIu9" crossorigin="anonymous">
<!-- <script src="/js/jquery.xdomainajax.js"></script> -->
<script src="/css/egovframework/test.js" type="text/javascript"></script>

<style type="text/css">



/* 달력 */
.Calendar td {
	height: 31px;
	line-height: 31px;
	font-size: 0.9em;
}
.Calendar { 
	width: 231px;
	height: 100px;
	text-align: center;
	margin: 0 auto; 
	border-spacing: 0px;
}
.Calendar>thead>tr:first-child>td { 
	font-weight: bold; 
}
.noCleanedDay{
	color: lightgrey;
	cursor: default;
}
.cleanedDay{           
	cursor: pointer;
}
.cleanedDay.selectedDay{            
	background: #ea8139;        
	color: #fff;
	cursor: pointer;
	border-radius: 31px;
}

/********************************/

*{
	/* margin:auto; */
	box-sizing: border-box;
	font-family: 'Noto Sans KR', sans-serif;
}

body{
	background-color: #000000b0;
}


/* 화면 너비가 768px 이하일 때 스타일 */
@media (max-width: 768px) {
    #mapContainer {
        display: block; /* 블록 레이아웃으로 변경 */
        width: 100%; /* 전체 너비로 확장 */
    }
    
    #sideTab {
        width: 100%; /* 전체 너비로 확장 */
        float: none; /* float 제거 */
        border-radius: 5px;
       
    }
}

#box3{
	display:none;
}

#maps{
	padding: 5px;
	background: ivory;
	border-radius: 5px;
}

ul{
	list-style: none;
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
    border-radius: 5px;
}

#totalRatio{
	width: 200px;
	height: 100px;
	border: 1px solid;
	float: right;
	margin-left: 350px;
}

#sideTab{
    

   	background-color: ivory;
    width: 470px;
    height: 800px;
    float: left;
    text-align: center;
    border-radius: 5px;
}

#logoImg{
	height: 50px;
    width: 50px;
    margin-right: 15px;	
}

.staicsBox{
	margin: 0 auto;
    width: 300px;
    background-color: white;
    border-radius: 20px;
    margin-top: 40px;
    display:none;
}

.roundbox{
    
    margin: 0 auto;
    width: 300px;
    background-color: white;
    border-radius: 20px;
    margin-top: 40px;
            
        }
        
 #box4{
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
	margin-top: 12px;
	background: ivory;
	
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
    position: absolute;
    margin-left: 300px;
    font-size: 13px;
    color: #00000091;
    background-color: transparent;
    margin-top: 11px;
    display: none;
}

#hybrid.on{
	 
    background-color: cadetblue;
    color: white;
      
}

#statis{
	border:1px solid;
}

#changeBtn{
	height: 450px;
	border-radius: 0;
}

#plusData{
	width: 58px; 
	height: 450px;
	border-radius: 0;
	
}

#title{
	font-size: 1.4em;
    font-weight: 600;
}

#btn_chart {
    color: #343a40;
    position: fixed;
    cursor: pointer;
    /* margin-top: 13px;
    margin-left: 90px; */
}
#btn_plus{
   border: 1px solid lightgrey;
   width: 25px;
   height: 25px;
   border-radius: 25px;
   background-color: rgb(226, 226, 226);   
   box-shadow: 3px 3px 3px lightgrey;
   text-align: center;
   line-height: 20px;
   font-weight: 600;
   float: left;
   margin-left: 15px;
}
#attach{
   cursor: pointer;
} 

#drag-drop-area{

	border: 1.9px dashed gray;
    text-align: center;
    padding-top: 14px;

}

 
/* 지도 종류 */

.tbl{
	border: 1px solid lightgrey;
	box-shadow: 3px 3px 3px lightgrey;
	border-radius: 5px;
	background-color: white;
	width: 90%;
	height: 40px;
	line-height: 40px;
	margin-top: 10px;
	margin-bottom: 20px;
	text-align: center;
	font-size: 0.9em;
	border-spacing: 0px;
	margin-left: 20px;
}
.tbl_title{
	border-right: 1px solid lightgrey;
	width: 25%;
	height: 100%;
	background-color: rgb(226, 226, 226);
}
.tbl_content{
	width: 25%;
	height: 100%;
	border-right: 1px solid lightgrey;
}
.tbl_content:hover{
	cursor: pointer;
	user-select: none;
}
.tbl_carlist_title{
	border-right: 1px solid lightgrey;
	background-color: rgb(226, 226, 226);
}
.tbl_carlist_content{
	width: 75%;
	height: 100%;
	border-bottom: 1px solid lightgrey;
}
.tbl_carlist_content:hover{
	cursor: pointer;
	user-select: none;
}
.tbl_noSelect:hover{
	cursor: default;
}
.tbl_selected{
	background-color: #0054a7;
	color: white;
	cursor: pointer;
}
.tbl_selected_hybrid{
	background-color: #00aeba;
	color: white;
	cursor: pointer;
}
.gu_selected{
	background-color: #0054a7;
	color: white;
	cursor: pointer;
}

/* 차트모달 스타일 */

#chartBox{
	display:flex;
}

#rightBox{
	width: 50%;
   	border-right: 2px solid #c1c1c1;
    margin-top: 70px;
}

#leftBox{
	width: 50%;
	display: grid;
}

#goLive{
	
	width: 50px;
    margin-left: 10px;
    margin-top: 10px;
}

#goStop{
	width: 50px;
    margin-left: 10px;
    margin-top: 10px;
}

/* new css */

	#icon_plus{
   border: 1px solid lightgrey;
   width: 25px;
   height: 25px;
   border-radius: 25px;
   background-color: rgb(226, 226, 226);   
   box-shadow: 3px 3px 3px lightgrey;
   text-align: center;
   line-height: 20px;
   font-weight: 600;
   float: left;
   margin-left: 15px;
}
#btn_addCar, #btn_addData, #btn_chart{
   cursor: pointer;
}
/* 모달창 */
.newModal {
    width: 400px;
    background-color: #fff;
    border: 1px solid lightgrey;
    border-radius: 5px;
}
.modal_title {
   padding: 13px;
   border-bottom: 1px solid lightgrey;
   font-size: 1em;
   font-weight: 600;
}
.modal_body {
   padding: 10px 20px 10px 20px;
   border-bottom: 1px solid lightgrey;
}
.modal_foot {
   padding: 10px;
   float: right;
}
input[name=car_num]{
   border: 1px solid lightgrey;
   border-radius: 5px;
   margin: 5px;
   margin-bottom: 15px;
   width: 350px;
   height: 30px;
   line-height: 30px;   
}
select[name=car_type]{
   border: 1px solid lightgrey;
   border-radius: 5px;
   margin: 5px;
   margin-bottom: 15px;
   width: 350px;
   height: 30px;
   line-height: 30px;   
}
.file-selector{
   border: 1px solid lightgrey;
   border-radius: 5px;
   margin: 5px;
   margin-bottom: 15px;
   width: 350px;
   height: 30px;
   line-height: 30px;
}
input[type=file]::file-selector-button{
   border: 1px solid transparent;
   border-right: 1px solid lightgrey;
   border-radius: 5px 0px 0px 5px;
   background-color: rgb(226, 226, 226);
   height: 30px;
   cursor: pointer;   
}
.btn_modal_submit{
   border: 1px solid #00aeba;
   border-radius: 5px;
   width: 50px;
   height: 30px;
   cursor: pointer;
   background-color: #00aeba; 
   color: #fff;
   margin-right: 3px;
}
.btn_modal_close{
   border: 1px solid #888;
   border-radius: 5px;
   width: 50px;
   height: 30px;
   cursor: pointer;
   background-color: #888; 
   color: #fff;
}
.info{
   font-size: 0.7em;
   color: red;
}

.btn_modal_login{
   border: 1px solid #00aeba;
   border-radius: 5px;
   width: 50px;
   height: 30px;
   cursor: pointer;
   background-color: #00aeba; 
   color: #fff;
   margin-right: 3px;
}

#btn_login{
	width: 80px; 
	background-color: #00aeba;
	border-color: #00aeba;
}

#id{
	margin-bottom: 10px;
    width: 100%;
    height: 50px;
}

#pw{
	width: 100%;
    height: 50px;
}

.large-input{
	font-size: 20px;
	padding: 10px;
}

#car_info{
	margin-top: 10px;
    position: absolute;
    width: 200px;
    height: 65px;
    margin-left: 20px;
}

#myDonutChart{
	margin-top: 70px;	
}


/*         */

</style>

</head>
<body>
		
		
				
		<div id="modal_login" class="newModal" style="display: none; width: 400px;">
            <div class="modal_title">로그인</div>
            
               <div class="modal_body">
                  
                  <input class="large-input" name="id" id="id" type="text" placeholder="아이디">	
                  <input class="large-input" name="pw" id="pw" type="password" placeholder="비밀번호">
                  
                 </div>
                 <div class="modal_foot">
                     <button type="button" id="btn_login" class="btn_modal_close">로그인</button>
                 </div>
             
           </div>		
					
				
				
				
	<input type="hidden" id="car">
	<input type="hidden" id="member" value="${member}">	
	<img id="goLive" alt="" src="/images/egovframework/example/live.JPG">
	<img id="goStop" alt="" src="/images/egovframework/example/stop.JPG" style="display:none;">
	<!--  <div><button id="changeBtn" >통계보기</button></div>-->
	
    <div id="mapContainer">
        
        <!-- 사이드메뉴 div -->
        <div id="sideTab">
        	  <div id='toptext'>
               <p><img id="logoImg" src="<c:url value='/images/egovframework/example/yongin.svg'/>" alt=""/><spring:message code="list.sample" /><b id="title">용인시 청소차 관제 시스템</b></p>
            </div> 
		
		
		
		<!-- new 지도 -->
		
		<table class="tbl">
				<tr>
					<th class="tbl_title tbl_noSelect">지도종류</th>
					<td class="tbl_content" id="normal">기본</td>
					<td class="tbl_content tbl_selected" id="wisung">위성</td>
					<td class="tbl_content on" id="hybrid">하이브리드</td>
				</tr>
			</table>
			
			
		<!-- 구역별 -->
		<table class="tbl">
				<tr>
					<th class="tbl_title tbl_noSelect">권역(구)</th>
					<td class="tbl_content" id="btn_gu_cheoin">처인구</td>
					<td class="tbl_content" id="btn_gu_giheung">기흥구</td>
					<td class="tbl_content" id="btn_gu_suji">수지구</td>
				</tr>
			</table>			
		
	
            
		<!-- new 차량 리스트 -->
		
		<table class="tbl" id="tbl_carlist">
		  <tr>
		    <th rowspan="5" class="tbl_carlist_title tbl_noSelect">차량</th>
		    <td class="tbl_carlist_content carbtnList" id="btn_car_0" data-car-num="${pList[0].car_num}" >${pList[0].car_num}</td>
		  </tr>
		  <c:forEach begin="1" items="${pList}" var="point" varStatus="loop">
		    <tr>
		      <td class="tbl_carlist_content carbtnList" id="btn_car_${loop.index}" data-car-num="${point.car_num}">${point.car_num}</td>
		    </tr>
		  </c:forEach>
		</table>
		
		
		
		<!-- 달력 -->
			<button id='close' onclick='close_calendar()'>접기</button>
			<div id="selectedCar_info" style="display: none;">
				
				<!--  <span id="btn_chart" data-bs-toggle="modal" data-bs-target="#exampleModal">통계보기 &raquo;</span> -->
				
				<table class="tbl">
					<tr>
						<th colspan="2" class="tbl_title tbl_noSelect" id="selectedCar_num" style="border-bottom: 1px solid lightgrey"></th>
					</tr>
					<tr>
						<th class="tbl_carlist_title tbl_noSelect" style="border-bottom: 1px solid lightgrey">날짜</th>
						<td class="tbl_carlist_content">
							<table class="Calendar">
								<thead>
									<tr>
										<td onClick="prevCalendar();" style="cursor:pointer;"><i class="fa-solid fa-chevron-left"></td>
										<td colspan="5" class="tbl_noSelect">
											<span id="calYear"></span>년
											<span id="calMonth"></span>월
										</td>
										<td onClick="nextCalendar();" style="cursor:pointer;"><i class="fa-solid fa-chevron-right"></td>
									</tr>
									<tr>
										<td>일</td>
										<td>월</td>
										<td>화</td>
										<td>수</td>
										<td>목</td>
										<td>금</td>
										<td>토</td>
									</tr>
								</thead>					
								<tbody></tbody>
							</table>	
						</td>
					</tr>
					<tr>
						<th class="tbl_carlist_title tbl_noSelect" style="border-bottom: 1px solid lightgrey">운행시간</th>
						<td class="tbl_carlist_content tbl_noSelect" id="clean_time"></td>
					</tr>
					<tr>
						<th class="tbl_carlist_title tbl_noSelect" style="border-bottom: 1px solid lightgrey">청소비율</th>
						<td class="tbl_carlist_content tbl_noSelect" id="clean_ratio"></td>
					</tr>
				</table>
			</div>	
					            
         
          <!-- 일별 통계 & 월별 통계 -->
		 <!-- 
		 <div class="staicsBox" id="box5">
	 		<div>
	 			<h5>통계</h5>
	 		</div>
	 		<hr>
	 		<div>
	 			<span>일별 통계</span><br>
	 			<span>월별 통계</span>
	 			
	 		</div>
		 </div>            
           -->
            
         <!-- 통계 차량 리스트 --> 
         
         <div class="staicsBox" id='box4'>
                <div id='carlist'>
                    <h5><b>차량 목록</b></h5> 
                    <hr>
                    <div id='staicsCarlist'>
                    </div>
                </div>                       
            </div>   
            
		
            
          <!-- 선택 차량 -->
        <!-- <div class='roundbox' id='box3'>
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
                       	<p>날짜를 선택 해주세요.</p>
                    </div>
                    <hr>
                    <button id='close'>접기</button>
                </div>
            </div> -->
            
            
            <!--  -->
            	<div style="position: fixed; margin-left: 9px;" >
		            <div id="btn_addCar" style="display: inline-block">
		               <div id="icon_plus"><i class="fa-solid fa-plus" style="font-size: 0.8em"></i></div><span style="margin-left: 10px; font-size: 0.9em">차량추가</span>
		            </div>
		            
		            <div id="btn_addData" style="display: inline-block">
		               <div id="icon_plus"><i class="fa-solid fa-plus" style="font-size: 0.8em"></i></div><span style="margin-left: 10px; font-size: 0.9em">데이터추가</span>
		            </div>
		                          
		            <div id="btn_chart" style="display: inline-block">
		               <div id="icon_plus"><i class="fa-solid fa-chart-line" style="font-size: 0.8em"></i></div><span style="margin-left: 10px; font-size: 0.9em">통계보기</span>
		            </div>
		         </div>
            
            
            <!-- 데이터 추가 원본 -->
               <!-- <div id="attach" style="display: inline-block" data-bs-toggle="modal" data-bs-target="#secondModal">
		            <div id="btn_plus">+</div><span style="margin-left: 10px; font-size: 0.9em;">데이터(.csv) 추가...</span>
		            <input type="file" style="display: none;">
	         </div> -->
        </div>
        
        
        
        
        <div id="maps" style="width: 100%; height: 850px; left: 0px; top: 0px"></div>
        
        <div id="statis" style="width: 100%; height: 900px; left: 0px; top: 0px; display: none; ">
			
			<!-- 선택차량, 현재 통계 표시 월 -->
			<div></div>
			
			    	   		
    	   		
    	    
      		
        </div>
    	
    </div>
    
    
    	<!-- 차량추가 모달 -->
	    <div id="modal_addCar" class="newModal" style="display: none">
	            <div class="modal_title">차량 추가</div>
	            <form id="form_addCar" name="form_addCar" action="/addCar">
	               <div class="modal_body">
	                  <span class="info"><i class="fa-solid fa-circle-info"></i> 차량번호를 입력하세요.</span>
	                  <input type="text" name="car_num" required>
	                  <span class="info"><i class="fa-solid fa-circle-info"></i> 차량유형을 선택하세요.</span>
	                  <select name="car_type" required>
	                     <option value="진공노면청소">진공노면청소</option>
	                     <option value="분진흡입">분진흡입</option>
	                  </select>
	                 </div>
	                 <div class="modal_foot">
	                     <button type="submit" class="btn_modal_submit">확인</button>
	                     <button type="button" class="btn_modal_close">취소</button>
	                 </div>
	             </form>
           </div>
         
         
         <!--  -->
         	<!-- 데이터 추가 모달 -->
         <div id="modal_addData" class="newModal" style="display: none">
            <div class="modal_title">데이터 추가</div>
            <form name="form_addData" method="post" enctype="multipart/form-data" action="/addData">
               <div class="modal_body">
                  <span class="info" id="gpsInfo"><i class="fa-solid fa-circle-info"></i> gps 데이터를 추가하세요.</span>
                     <input type="file" id="gpsInput" class="file-selector" name="file" accept=".csv">
                     <span class="info" id="rpmInfo"><i class="fa-solid fa-circle-info"></i> rpm 데이터를 추가하세요.</span>
                     <input type="file" id="rpmInput" class="file-selector" name="file" accept=".csv">
                     <span class="info" id="noiseInfo"><i class="fa-solid fa-circle-info"></i> noise 데이터를 추가하세요.</span>
                     <input type="file" id="noiseInput" class="file-selector" name="file" accept=".csv">
                 	 
                 	 <div id="drag-drop-area">
					    <p>또는 여기로 파일을 드래그하세요.</p>
					    <input type="file" id="file-input" accept=".csv" multiple style="display:none;">
					</div>
					
					
            		<script>
            		// 드래그 앤 드랍 파일 처리 스크립트
            		document.addEventListener("DOMContentLoaded", function() {
            		    
            		    var dropArea = document.getElementById('drag-drop-area');
            		    var gpsInput = document.getElementById('gpsInput');
            		    var rpmInput = document.getElementById('rpmInput');
            		    var noiseInput = document.getElementById('noiseInput');

            		    ['dragenter', 'dragover', 'dragleave', 'drop'].forEach(eventName => {
            		        dropArea.addEventListener(eventName, preventDefaults, false);
            		    });

            		    function preventDefaults(e) {
            		        e.preventDefault();
            		        e.stopPropagation();
            		    }

            		    dropArea.addEventListener('drop', handleDrop, false);

            		    function handleDrop(e) {
            		        var dt = e.dataTransfer;
            		        var files = dt.files;

            		        handleFiles(files);
            		    }

            		    function handleFiles(files) {
            		        ([...files]).forEach(async file => {
            		            const content = await file.text();
            		            let dt = new DataTransfer();
            		            dt.items.add(file);
            		                
            		            if (content.includes('latitude') || content.includes('longitude') 
            		            		|| content.includes('lon') || content.includes('lat')) {
            		                gpsInput.files = dt.files;
            		                $('#gpsInfo').attr('style','display:none;');
            		            } else if (content.includes('rpm')) {
            		                rpmInput.files = dt.files;
            		                $('#rpmInfo').attr('style','display:none;');
            		            } else if (content.includes('noise')) {
            		                noiseInput.files = dt.files;
            		                $('#noiseInfo').attr('style','display:none;');
            		            }
            		        });
            		    }

            		    
            		    gpsInput.addEventListener('change', function() {
            		        validateFile(this, 'gps');
            		    });

            		    rpmInput.addEventListener('change', function() {
            		        validateFile(this, 'rpm');
            		    });

            		    noiseInput.addEventListener('change', function() {
            		        validateFile(this, 'noise');
            		    });

            		    async function validateFile(inputElement, fileType) {
            		        const file = inputElement.files[0];
            		        if (file) {
            		            const content = await file.text();

            		            switch (fileType) {
            		                case 'gps':
            		                    if (!content.includes('latitude') || !content.includes('longitude') 
                    		            		|| !content.includes('lon') || !content.includes('lat')) {
            		                        alert('파일을 확인해 주세요');
            		                        inputElement.value = ''; // 파일 선택을 초기화합니다.
            		                        $('#gpsInfo').attr('style','display:;');
            		                    }
            		                    break;
            		                case 'rpm':
            		                    if (!content.includes('rpm')) {
            		                        alert('파일을 확인해 주세요');
            		                        inputElement.value = ''; // 파일 선택을 초기화합니다.
            		                        $('#rpmInfo').attr('style','display:;');
            		                    }
            		                    break;
            		                case 'noise':
            		                    if (!content.includes('noise')) {
            		                        alert('파일을 확인해 주세요');
            		                        inputElement.value = ''; // 파일 선택을 초기화합니다.
            		                        $('#noiseInfo').attr('style','display:;');
            		                    }
            		                    break;
            		            }
            		        }
            		    }
            		    
            		    $('#gpsInput').change(function(){
            		    	
                		    if (gpsInput.files != null) {
                		        var fileName = gpsInput.files[0].name;
                		        console.log(fileName); // 파일 이름을 콘솔에 출력
                		        $('#gpsInfo').attr('style','display:none;');
                		    }
            		    });
            		    $('#rpmInput').change(function(){
            		    	
                		    if (rpmInput.files.length > 0) {
                		        var fileName = rpmInput.files[0].name;
                		        console.log(fileName); // 파일 이름을 콘솔에 출력
                		        $('#rpmInfo').attr('style','display:none;');
                		    }
            		    });
            		    $('#noiseInput').change(function(){
            		    
                		    if (noiseInput.files.length > 0) {
                		        var fileName = noiseInput.files[0].name;
                		        console.log(fileName); // 파일 이름을 콘솔에 출력
                		        $('#noiseInfo').attr('style','display:none;');
                		    }
            		    });
        		    	
        		    

            		});

            		

            		
            		$(document).ready(function() {
            		    $('form[name="form_addData"]').on('submit', function(e) {
            		        e.preventDefault();

            		        // 현재의 form 데이터를 가져옵니다.
            		        var formData = new FormData(this);

            		        $.ajax({
            		            url: '/addData',
            		            type: 'POST',
            		            data: formData,
            		            processData: false,  // 필요한 설정입니다. 이것을 false로 설정해야 FormData를 사용할 수 있습니다.
            		            contentType: false,  // contentType을 false로 설정하면 jQuery가 contentType을 설정하지 않습니다.
            		            success: function(data) {
            		                if (data === 'success') {
            		                    alert('데이터가 성공적으로 추가되었습니다.');
            		                    location.href='/test';
            		                } else {
            		                    alert('데이터 추가에 실패했습니다.');
            		                }
            		            },
            		            error: function() {
            		                alert('서버 에러가 발생했습니다.');
            		            }
            		        });
            		    });
            		});
            		    
            		    
            		
            		
            		</script>
            		
            		
                    
                     
                 </div>
                 <div class="modal_foot">
                     <button type="submit" class="btn_modal_submit">확인</button>
                     <button type="button" class="btn_modal_close">취소</button>
                 </div>
             </form>
           </div>

         <!--  -->  

         <!-- 데이터 추가 모달 -->
         <!-- <div id="modal_addData" class="newModal" style="display: none">
            <div class="modal_title">데이터 추가</div>
            <form name="form_addData" method="post" enctype="multipart/form-data" action="/addData">
               <div class="modal_body">
                  <span class="info"><i class="fa-solid fa-circle-info"></i> gps 데이터를 추가하세요.</span>
                     <input type="file" class="file-selector" name="file_gps" accept=".csv" required>
                     <span class="info"><i class="fa-solid fa-circle-info"></i> rpm 데이터를 추가하세요.</span>
                     <input type="file" class="file-selector" name="file_rpm" accept=".csv" required>
                     <span class="info"><i class="fa-solid fa-circle-info"></i> noise 데이터를 추가하세요.</span>
                     <input type="file" class="file-selector" name="file_noise" accept=".csv" required>
                     
                     <div id="drop_zone">여기에 파일을 드래그 하세요.</div>

	               	이 폼은 필요하지만 실제로는 숨겨져 있음
	               <form id="uploadForm" name="uploadForm" enctype="multipart/form-data" style="display:none;">
	                   <input type="file" multiple name="file" id="file" />
	                   <input type="button" value="업로드" id="uploadButton" />
               		</form>
                     
                 </div>
                 <div class="modal_foot">
                     <button type="submit" class="btn_modal_submit">확인</button>
                     <button type="button" class="btn_modal_close">취소</button>
                 </div>
             </form>
           </div> -->
    
    	<!-- 통계 모달창 추가 -->
    	<div id="modal_chart" class="newModal" style="display: none; width: 1200px;">
            <div class="modal_title">통계</div>
            
               <div class="modal_body" style="display: flex;">
                <div id="car_info">
                	<span id="car_info_span" style="border-bottom: 2px solid #2b272761;">차량정보 : </span><br>                	                	
                </div>  	
                  	<div id="rightBox">
	         		 <div id="my1">
    		 		<canvas id="ctx" width="500" height="500"></canvas>
   				</div>
          </div>
          
          <div id="leftBox" >
          	<!-- <div id="totalRatio">
				<h5>월별 통계</h5>
				
				<span><b>월 운행시간 :</b><b id="monthTime"></b></span><br>
				<span><b>월 청소비율 :</b><b id="monthRatio"></b></span>
									
			</div> -->
          	  <div id="my2" style="margin: 0 auto;">
      			<canvas id="myDonutChart" width="400" height="400"></canvas>	
      		  </div>
      				<span id="drivingDate" style="position: absolute; margin-left: 208px; margin-top: 545px;">운행일자 : </span>
          </div>
                  	
                  	
                 </div>
                 <div class="modal_foot">
                     <button type="button" class="btn_modal_close">취소</button>
                 </div>
             
           </div>

  
  <!-- 데이터 추가 모달창 -->
  <!-- <div class="modal fade" id="secondModal" tabindex="-1" aria-labelledby="exampleModalLabel" aria-hidden="true" data-target="#modal2">
	    <div class="modal-dialog modal-dialog-centered">
	      <div class="modal-content">
	        <div class="modal-header">
	          <h1 class="modal-title fs-5" id="exampleModalLabel">데이터 추가</h1>
          <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
        </div>
     <div class="modal-body">
          
        
        
        <div id="drop_zone">여기에 파일을 드래그 하세요.</div>

               이 폼은 필요하지만 실제로는 숨겨져 있음
               <form id="uploadForm" name="uploadForm" enctype="multipart/form-data" style="display:none;">
                   <input type="file" multiple name="file" id="file" />
                   <input type="button" value="업로드" id="uploadButton" />
               </form> 
        </div>
        
        <div class="modal-footer">
          <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button>
        </div>
      </div>
    </div>
  </div>  -->   
  
  			                  
   <script>
   
   var close = document.querySelector('#close'); // 접기버튼선택
   const input_id = document.querySelector('#id');
   const input_pw = document.querySelector('#pw'); 
   const btn_login = document.querySelector('#btn_login');
   
   function close_calendar(){
	   var selectedCar_info = document.querySelector("#selectedCar_info");
	   selectedCar_info.style.display = "none";
	   close.style.display = "none";
   }
   
   input_id.addEventListener("keydown", function (event) {
	    if (event.key === "Enter") {
	        // Prevent the default Enter key behavior (e.g., submitting a form)
	        event.preventDefault();
	        
	        // Simulate a click event on the submit button
	        btn_login.click();
	    }
	});
   
   input_pw.addEventListener("keydown", function (event) {
	    if (event.key === "Enter") {
	        // Prevent the default Enter key behavior (e.g., submitting a form)
	        event.preventDefault();
	        
	        // Simulate a click event on the submit button
	        btn_login.click();
	    }
	});
   
   
// 차량추가
   var btn_addCar = document.getElementById('form_addCar'); 
   var modal_addCar = document.getElementById('modal_addCar');		
		
   
	btn_addCar.addEventListener('submit', function (e) {
       
		var bg = document.querySelector("#bg");
		
		e.preventDefault(); // Prevent the default form submission

       // Create a new FormData object from the form
       const formData = new FormData(this);

       // Use the fetch API to send the formData to the server
       fetch('/addCar', {
		    method: 'POST',
		    headers: {
		        'Content-Type': 'application/json'
		    },
		    body: JSON.stringify({
		        car_num: formData.get('car_num'),
		        car_type: formData.get('car_type')
		    })
		})
       .then(response => {
           if (response.ok) {
        	   modal_addCar.style.display = "none";
        	   bg.remove();
        	   alert("차량추가가 완료되었습니다.")
        	   
        	   
        	   location.reload(); // 페이지 새로고침
               console.log('Data submitted successfully');
           } else {
               // Handle error response (e.g., show an error message)
               console.error('Error submitting data');
           }
       })
       .catch(error => {
           // Handle network error
           console.error('Network error:', error);
       });
   });
   
   // new modal 창 
   
   	window.addEventListener("load", function(){
   		
	
	
	
	//////////////////////////////////
   		
   	
   	var member = document.querySelector("#member").value;	
   
   	// 세션에서 member 정보를 가져오도록 함.
   /* if(${member} === null){
	  var loginModal = document.querySelector("#modal_login");
	   */
	   if(member == ''){
		   loginModal("#modal_login");   
	   }
		      
   // };
   	
   		
   	document.querySelector("#btn_addCar").addEventListener("click", function() {
      showModal("#modal_addCar");
   });
   
   document.querySelector("#btn_addData").addEventListener("click", function() {
      showModal("#modal_addData");
   });
   
   document.querySelector('#btn_chart').addEventListener("click", function(){
	  showModal("#modal_chart"); 
   });
   
   
})

function loginModal(id){
   	 
	  
	   
	   let zIndex = 999;
     let modal = document.querySelector(id);

      // 모달 div 뒤 bg 레이어
     let bg = document.createElement("div");
     bg.setAttribute("id", "bg");
      bg.setStyle({
          position: "fixed",
          zIndex: zIndex,
          left: "0px",
          top: "0px",
          width: "100%",
          height: "100%",
          overflow: "auto",
          backgroundColor: "rgba(0,0,0,0.4)"
          
      });
      
      let mapCon = document.querySelector('#mapContainer');

      
      mapCon.setStyle({
    	  filter : "blur(7px)"
      });
      
      
      document.body.append(bg);
      
      modal.setStyle({
          position: "fixed",
          display: "block",
          boxShadow: "0 4px 8px 0 rgba(0, 0, 0, 0.2), 0 6px 20px 0 rgba(0, 0, 0, 0.2)",

          // bg 레이어 보다 한칸 위에 보이기
          zIndex: zIndex+1,

          // div 가운데 정렬
          top: "50%",
          left: "50%",
          transform: "translate(-50%, -50%)",
          msTransform: "translate(-50%, -50%)",
          webkitTransform: "translate(-50%, -50%)"
      });
			
      // 취소 버튼 누르면 bg 레이어와 모달 div 닫기
      modal.querySelector(".btn_modal_close").addEventListener("click", function() {
    	  
    	   var member = document.querySelector("#member");
    	  // id
	   	   var id = document.querySelector('#id').value;
	   	   
    	  // 비밀번호
	   	   var pw = document.querySelector('#pw').value;
	   	   console.log("id : ", id);
	   	   console.log("pw : ", pw);
    	  
    	  obj = {
    			  "id" : id,
    			  "pw" : pw
    	  }
    	  
    	  fetchPost2('/loginAction', obj, (map)=>{
    		  
    		  console.log(map);
    		  
    		  if(map.result == "success"){
    			
    			  member.value = id;
    			  bg.remove();
    	          mapCon.setStyle({
    	        	  filter : "blur(0px)"
    	          });
    	          modal.style.display = "none";
				
    	          
    		  }else{
    			  
    			  alert("로그인에 실패하였습니다.");
    		  }

    	  })

          
      });

     
   
   }


function showModal(id){
   let zIndex = 999;
   let modal = document.querySelector(id);

    // 모달 div 뒤 bg 레이어
   let bg = document.createElement("div");
   bg.setAttribute("id", "bg");
    bg.setStyle({
        position: "fixed",
        zIndex: zIndex,
        left: "0px",
        top: "0px",
        width: "100%",
        height: "100%",
        overflow: "auto",
        backgroundColor: "rgba(0,0,0,0.4)",
        filter: "blur(50px)"
    });
    document.body.append(bg);

    // 취소 버튼 누르면 bg 레이어와 모달 div 닫기
    modal.querySelector(".btn_modal_close").addEventListener("click", function() {
        bg.remove();
        modal.style.display = "none";
    });

    modal.setStyle({
        position: "fixed",
        display: "block",
        boxShadow: "0 4px 8px 0 rgba(0, 0, 0, 0.2), 0 6px 20px 0 rgba(0, 0, 0, 0.2)",

        // bg 레이어 보다 한칸 위에 보이기
        zIndex: zIndex+1,

        // div 가운데 정렬
        top: "50%",
        left: "50%",
        transform: "translate(-50%, -50%)",
        msTransform: "translate(-50%, -50%)",
        webkitTransform: "translate(-50%, -50%)"
    });
}

// Element에 style 한번에 오브젝트로 설정하는 함수 추가
Element.prototype.setStyle = function(styles) {
    for (let k in styles) this.style[k] = styles[k];
    return this;
};
   
   // 
   
   
   
   
	// Add event listeners to all elements with class 'carbtnList'
	   const carListElements = document.querySelectorAll(".carbtnList");
	   const carInput = document.getElementById("car");
		var close = document.querySelector('#close');	
	   
	   carListElements.forEach((element) => {
	     element.addEventListener("click", () => {
	    	close.style.display = 'block';
	       // Get the car number from the 'data-car-num' attribute
	       const carNum = element.getAttribute("data-car-num");
	
	       ////////////////////////////////////////////////
	       	var clean_time = document.getElementById("clean_time");	
	       	var clean_ratio = document.getElementById("clean_ratio");
	       
	       // 기존의 clean_o, clean_x, beginPoint, endPoint, course 레이어를 삭제한다
				     maps.getLayers().getArray()
						  .filter(layer => layer.get('name')==='clean_o')
						  .forEach(layer => maps.removeLayer(layer));    
					    maps.getLayers().getArray()
						  .filter(layer => layer.get('name')==='clean_x')
						  .forEach(layer => maps.removeLayer(layer));
					    maps.getLayers().getArray()
						  .filter(layer => layer.get('name')==='start_point')
						  .forEach(layer => maps.removeLayer(layer));
					    maps.getLayers().getArray()
						  .filter(layer => layer.get('name')==='end_point')
						  .forEach(layer => maps.removeLayer(layer));
					    maps.getLayers().getArray()
						  .filter(layer => layer.get('name')==='clean_route')
						  .forEach(layer => maps.removeLayer(layer));
				    
				    maps.getView().animate({
				        center: ol.proj.transform([127.1775537, 37.2410864], 'EPSG:4326', 'EPSG:900913'),
				        zoom: 11.5,
				        duration: 800
				    }); 
					    
					// 기존의 운행시간, 청소비율을 삭제한다
					clean_time.innerText = "";
					clean_ratio.innerText = "";			

				    // 줌아웃하고 중심 좌표를 이동한다
				   
	       
	       
	       ////////////////////////////////////////////////
	       
	       
	       // Update the input value with the selected car number
	       carInput.value = carNum;
	     });
	   });
   
   
   
   		/// 차트삭제
   		
	   
	   window.addEventListener("load", function() {
		  
		// 차량목록에서 특정 차량을 선택하면
			for(let i=0; i<${pList.size()}; i++){
				
				let selectedCar = document.querySelector("#btn_car_" + i);		
				selectedCar.addEventListener("click", function(){
					
					// 모든 차량목록의 배경색을 흰색으로 바꾼다
					let car = document.getElementsByClassName("tbl_carlist_content");
					for(let j=0; j<car.length; j++){
						car[j].style.background = "white";
						car[j].style.color = "black";
					}
					
					// 차량목록 중 선택한 차량의 배경색을 남색으로 바꾼다
					selectedCar.style.background = "#0054a7";
					selectedCar.style.color = "white";
					
					// 차량 정보를 보여준다
					selectedCar_info.style.display = "block";
					selectedCar_num.innerHTML = `<i class="fa-solid fa-truck-front"></i> ` + selectedCar.innerText;
					
					// 선택 가능한 날짜를 불러온다
					getDateList(selectedCar_num.innerText.trim());
	
				})
			}
		   	   
		   
		   // 맵 & 통계 화면 변경.
		   var changeBtn = document.getElementById("changeBtn");
		   var maps = document.getElementById("maps");
		   var statis = document.getElementById("statis");
		   var mapView = document.querySelector(".ol-viewport");
		   var box4 = document.querySelector("#box4");   
		   // 사이드탭 라운드 박스들
		   var roundbox = document.querySelectorAll(".roundbox");
		   var staicsBox = document.querySelectorAll(".staicsBox");
		   var box1 = document.getElementById("box1");
		   
		   // 월별 청소시간 출력필요 변수
		   var monthTime = document.querySelector("#monthTime");
		   var monthRatio = document.querySelector("#monthRatio");
		   
		   
		   var btn_chart = document.getElementById("btn_chart");
		   
		   // 모드변경
		   btn_chart.addEventListener("click", function(){
			   
			   
			   
			   var barrr = document.querySelector("#my1")
			   
			   	barrr.innerHTML ='<canvas id="ctx" width="500" height="500"></canvas>';
			   
			   var my = document.querySelector("#my2");
		  		
		  		my.innerHTML ='<canvas id="myDonutChart" width="400" height="500"></canvas>';
			   
			   var date = calYear.innerText + "-" + leftPad(calMonth.innerText)
			   var car_num = document.querySelector("#car").value;
			   
			   var car_info_span = document.querySelector('#car_info_span');
			   car_info_span.innerHTML = '차량정보 : ' + car_num;
			   
			   var obj = {
						  'car_num' : car_num,
						  'date'	: date
				   }
			   
			   console.log("헤헤헤");
			   
			   var ctx = document.getElementById('ctx');   
			   
			   ctx.style.width = 400;
			   ctx.style.height = 100;
  	 	
		  	 	fetchPost("/chart", obj)
		  	  .then(map => {
		  		  
		  		  // 데이터가 없는 경우??		  
		  		/*   if(map.vo.length == 0){
		  			  alert('데이터가 없습니다.');
		  			 	
		  			  return;
		  		  } */
		  		  
		  		  
		  	    console.log('map.vo : ',map.vo);
		  	    console.log('map.monthData : ',map.monthData);
		  	    console.log('map.cleanTime : ', map.cleanTime);
		  	  console.log('map.cleanRatio : ', map.cleanRatio);
		  	    
		  	    //monthRatio.innerHTML = ''+map.monthData.ratio+''
		  	  	//monthTime.innerHTML = ''+map.monthData.time+''
		  	    
		  	 const chart = new Chart(ctx, {
    type: 'bar',
    data: {
        labels: map.date,
        datasets: [{
            label: '청소시간 (분)',
            data: map.cleanTime,
            barThickness: 40,
            borderWidth: 3,
            backgroundColor: Array(map.date.length).fill('cadetblue'), // Initialize with cadetblue color
            borderColor: Array(map.date.length).fill('cadetblue'), // Initialize with cadetblue color
        }]
    },
    options: {
        layout: {
            padding: {
                left: 10,
                right: 10,
                top: 10,
                bottom: 10
            }
        },
        scales: {
            y: {
                beginAtZero: true
            }
        },
        responsive: false,
        width: 400,
        height: 300
    }
});

const canvas = chart.canvas;

canvas.addEventListener('click', function (event) {
    var my = document.querySelector("#my2");
    my.innerHTML = '<canvas id="myDonutChart" width="400" height="500"></canvas>';

    const bar = chart.getElementsAtEventForMode(event, 'nearest', { intersect: true });

    if (bar.length > 0) {
        const index = bar[0].index;
        const drivingRatio = map.cleanRatio[index];
        var drivingDate = document.querySelector('#drivingDate');
        drivingDate.innerHTML = '운행일자 : ' + map.date[index];

        // Reset the color of all bars to cadetblue
        chart.data.datasets[0].backgroundColor = Array(map.date.length).fill('cadetblue');
        chart.data.datasets[0].borderColor = Array(map.date.length).fill('cadetblue');

        // Set the color of the clicked bar to red
        chart.data.datasets[0].backgroundColor[index] = '#ea8139';
        chart.data.datasets[0].borderColor[index] = '#ea8139';

        chart.update();
			        	    
		  	  const totalHours = 100; // Total hours (assuming 24 hours in a day)

		        // Calculate non-driving time as the remaining hours
		        const nonDrivingRatio = totalHours - drivingRatio;
		        
		        

		        // Create the data object for the donut chart
		        const data = {
		                labels: ['운행비율', '비 운행비율'],
		                datasets: [{
		                    data: [drivingRatio, nonDrivingRatio],
		                    backgroundColor: ['cadetblue', '#ea8139'],
		                    borderColor: ['cadetblue', '#ea8139'],
		                    borderWidth: 1
		                }]
		            };

		            const ratioDoughnut = document.getElementById('myDonutChart').getContext('2d');
		            const myDonutChart = new Chart(ratioDoughnut, {
		                type: 'doughnut',
		                data: data,
		                options: {
		                    responsive: false,
		                    maintainAspectRatio: true,
		                    cutout: '70%',
		                    plugins: {
		                        legend: {
		                            position: 'top',
		                            labels: {
		                                boxWidth: 10,
		                                fontStyle: 'bold'
		                            }
		                        },
		                        tooltip: {
		                            callbacks: {
		                                label: function(context) {
		                                    let label = context.label || '';
		                                    if (label) {
		                                        label += ': ';
		                                    }
		                                    label += Math.round(context.parsed / totalHours * 100) + '%';
		                                    return label;
		                                }
		                            }
		                        }
		                    }
		                }
		            });
		  	    
		  	  }
		  	});
		  	    
		  	
		  	  
		  		// 청소 비율 chart
		  	
	 	
		  	// Sample data for driving and non-driving time in hours
		        const drivingRatio = map.cleanRatio[0]; // Replace with your actual driving time
		        const totalHours = 100; // Total hours (assuming 24 hours in a day)

		        // Calculate non-driving time as the remaining hours
		        const nonDrivingRatio = totalHours - drivingRatio;
		        
		        drivingDate.innerHTML = '운행일자 : ' + map.date[0];

		        // Create the data object for the donut chart
		        const data = {
		                labels: ['운행비율', '비 운행비율'],
		                datasets: [{
		                    data: [drivingRatio, nonDrivingRatio],
		                    backgroundColor: ['cadetblue', '#ea8139'],
		                    borderColor: ['cadetblue', '#ea8139'],
		                    borderWidth: 1
		                }]
		            };

		            const ratioDoughnut = document.getElementById('myDonutChart').getContext('2d');
		            const myDonutChart = new Chart(ratioDoughnut, {
		                type: 'doughnut',
		                data: data,
		                options: {
		                    responsive: false,
		                    maintainAspectRatio: true,
		                    cutout: '70%',
		                    plugins: {
		                        legend: {
		                            position: 'top',
		                            labels: {
		                                boxWidth: 10,
		                                fontStyle: 'bold'
		                            }
		                        },
		                        tooltip: {
		                            callbacks: {
		                                label: function(context) {
		                                    let label = context.label || '';
		                                    if (label) {
		                                        label += ': ';
		                                    }
		                                    label += Math.round(context.parsed / totalHours * 100) + '%';
		                                    return label;
		                                }
		                            }
		                        }
		                    }
		                }
		            });
		  		})
			        
		  	  
		  	  .catch(error => {
		  	    console.error(error);
		  	  });
		  	 	
		  	 	
		  	 
		  	 	
		  	 	
		  	 	
		   });
		     
		   
		 });
	   
	   
	   // 자동차 리스트 불러오기
	   function getCarList(map){
		   
		   var staicsCarlist = document.querySelector("#staicsCarlist");
		   var list = "<ul>";
		   
		   console.log("test ============================")
		   console.log(map);
		   
		   map.list.forEach(function (item, index){
			   
			   console.log(item.car_num);
			   
			   list += '<li>'+ item.car_num +'</li>';
			   
			   
		   })
		   console.log(list);
		   staicsCarlist.innerHTML = list;
	   }

   
   /*
      http://openlayers.org/en/latest/examples/wmts-layer-from-capabilities.html?q=WMTSCapabilities
   */
   
   window.addEventListener("load", function(){
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
	   var _this = this; 
	   maps.getLayers().forEach(function(layer){
            if(layer.get("name")=="vworld"){
            	 normal.className ="tbl_content";
      			_this.className ="tbl_content tbl_selected";
                layer.setSource(VworldSatellite)
            }
        })
        
        //normal.classList.remove("active");
        //wisung.classList.add("active");
   }

   normal.onclick=function(){
	   var _this = this;
	   maps.getLayers().forEach(function(layer){
            if(layer.get("name")=="vworld"){
            	 wisung.className ="tbl_content";
     			_this.className ="tbl_content tbl_selected";
            	layer.setSource(VworldBase)
            }
        })
       
        //normal.classList.add("active");
        //wisung.classList.remove("active");
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
         //center: ol.proj.transform([127.1775537, 37.2410864], 'EPSG:4326', 'EPSG:900913'),
         center: ol.proj.transform([127.1775537, 37.2410864], 'EPSG:4326', 'EPSG:3857'),
         zoom: 11,
         minZoom : 0,
         maxZoom : 21
      })
   });

   // 추가한 레이어
   var boundary = new ol.layer.Tile({
    source: new ol.source.TileWMS({
            url: 'http://182.222.169.37:8181/geoserver/opengis/wms',
            params: {
                'LAYERS': 'opengis:youngin',
                'TILED': true,
            },
            serverType: 'geoserver',
        })
     })
	 
     maps.addLayer(boundary);
		
		
		var gu_cheoin = new ol.layer.Tile({
	        source: new ol.source.TileWMS({
	            //Vworld Tile 변경
	            url: 'http://182.222.169.37:8181/geoserver/opengis/wms',
	            params: {
	            'layers' : 'opengis:gu_cheoin',
	            'tiled' : 'true'
	            },
	            serverType: 'geoserver'
	        }),
	        name: 'gu_cheoin'
	    })

	    var gu_giheung = new ol.layer.Tile({
	        source: new ol.source.TileWMS({
	            //Vworld Tile 변경
	            url: 'http://182.222.169.37:8181/geoserver/opengis/wms',
	            params: {
	            'layers' : 'opengis:gu_giheung',
	            'tiled' : 'true'
	            },
	            serverType: 'geoserver'
	        }),
	        name: 'gu_giheung'
	    })

		var gu_suji = new ol.layer.Tile({
	        source: new ol.source.TileWMS({
	            //Vworld Tile 변경
	            url: 'http://182.222.169.37:8181/geoserver/opengis/wms',
	            params: {
	            'layers' : 'opengis:gu_suji',
	            'tiled' : 'true'
	            },
	            serverType: 'geoserver'
	        }),
	        name: 'gu_suji'
	    })
	    

		btn_gu_cheoin.onclick = function () {
		    if (btn_gu_cheoin.className === "tbl_content gu_selected") {
		        btn_gu_cheoin.className = "tbl_content";
		        maps.addLayer(boundary);
		        maps.removeLayer(gu_cheoin);
		        maps.removeLayer(gu_giheung);
		        maps.removeLayer(gu_suji);
		        maps.getView().animate({
		            center: ol.proj.transform([127.1775537, 37.2410864], 'EPSG:4326', 'EPSG:900913'),
		            zoom: 11,
		            duration: 800
		        });
		    } else {
		        btn_gu_cheoin.className = "tbl_content gu_selected";
		        btn_gu_giheung.className = "tbl_content";
		        btn_gu_suji.className = "tbl_content";
		        maps.removeLayer(boundary);
		        maps.addLayer(gu_cheoin);
		        maps.removeLayer(gu_giheung);
		        maps.removeLayer(gu_suji);
		        maps.getView().animate({
		            center: ol.proj.transform([127.2529331499, 37.2033318957], 'EPSG:4326', 'EPSG:900913'),
		            zoom: 11.5,
		            duration: 800
		        });
		    }
		}

		btn_gu_giheung.onclick = function () {
		    if (btn_gu_giheung.className === "tbl_content gu_selected") {
		        btn_gu_giheung.className = "tbl_content";
		        maps.addLayer(boundary);
		        maps.removeLayer(gu_cheoin);
		        maps.removeLayer(gu_giheung);
		        maps.removeLayer(gu_suji);
		        maps.getView().animate({
		            center: ol.proj.transform([127.1775537, 37.2410864], 'EPSG:4326', 'EPSG:900913'),
		            zoom: 11,
		            duration: 800
		        });
		    } else {
		        btn_gu_cheoin.className = "tbl_content";
		        btn_gu_giheung.className = "tbl_content gu_selected";
		        btn_gu_suji.className = "tbl_content";
		        maps.removeLayer(boundary);
		        maps.removeLayer(gu_cheoin);
		        maps.addLayer(gu_giheung);
		        maps.removeLayer(gu_suji);
		        maps.getView().animate({
		            center: ol.proj.transform([127.1213408459, 37.2674315832], 'EPSG:4326', 'EPSG:900913'),
		            zoom: 11.5,
		            duration: 800
		        });
		    }
		}

		btn_gu_suji.onclick = function () {
		    if (btn_gu_suji.className === "tbl_content gu_selected") {
		        btn_gu_suji.className = "tbl_content";
		        maps.addLayer(boundary);
		        maps.removeLayer(gu_cheoin);
		        maps.removeLayer(gu_giheung);
		        maps.removeLayer(gu_suji);
		        maps.getView().animate({
		            center: ol.proj.transform([127.1775537, 37.2410864], 'EPSG:4326', 'EPSG:900913'),
		            zoom: 11,
		            duration: 800
		        });
		    } else {
		        btn_gu_cheoin.className = "tbl_content";
		        btn_gu_giheung.className = "tbl_content";
		        btn_gu_suji.className = "tbl_content gu_selected";
		        maps.removeLayer(boundary);
		        maps.removeLayer(gu_cheoin);
		        maps.removeLayer(gu_giheung);
		        maps.addLayer(gu_suji);
		        maps.getView().animate({
		            center: ol.proj.transform([127.0715510732, 37.3334474297], 'EPSG:4326', 'EPSG:900913'),
		            zoom: 11.5,
		            duration: 800
		        });
		    }
		}
		
		
		
   })

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
	 function fetchPost2(url, obj, callback){
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
	} 
    </script>
    
    
     <!-- <script>
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
               </script> -->
               
               <script>
               	// 달력 부분 
               	
               	function getDateList(car_num){
						fetchGet("/dateList?car_num=" + car_num, buildCalendar);
					}
					
					let nowMonth = new Date();  // 현재 달을 페이지를 로드한 날의 달로 초기화
					let today = new Date();     // 페이지를 로드한 날짜를 저장
					today.setHours(0,0,0,0);    // 비교 편의를 위해 today의 시간을 초기화
					
					// 달력 생성 : 해당 달에 맞춰 테이블을 만들고, 날짜를 채워 넣는다.
					function buildCalendar(result) {
						console.log(result,'출력=====================')
						let firstDate = new Date(nowMonth.getFullYear(), nowMonth.getMonth(), 1);     // 이번달 1일
						let lastDate = new Date(nowMonth.getFullYear(), nowMonth.getMonth()+1, 0);  // 이번달 마지막날
					
						let tbody_Calendar = document.querySelector(".Calendar > tbody");
						document.getElementById("calYear").innerText = nowMonth.getFullYear();             // 연도 숫자 갱신
						document.getElementById("calMonth").innerText = nowMonth.getMonth()+1;  // 월 숫자 갱신
					
						while (tbody_Calendar.rows.length>0) {                        // 이전 출력결과가 남아있는 경우 초기화
							tbody_Calendar.deleteRow(tbody_Calendar.rows.length-1);
						}
					
						let nowRow = tbody_Calendar.insertRow();        // 첫번째 행 추가
					
						for (let i=0; i<firstDate.getDay(); i++) {  // 이번달 1일의 요일만큼
							let nowColumn = nowRow.insertCell();        // 열 추가
						}
					
						for (let nowDay=firstDate; nowDay<=lastDate; nowDay.setDate(nowDay.getDate()+1)) {   // day는 날짜를 저장하는 변수, 이번달 마지막날까지 증가시키며 반복  
							let nowColumn = nowRow.insertCell();        // 새 열을 추가하고
							nowColumn.innerText = nowDay.getDate();      // 추가한 열에 날짜 입력
					
							if (nowDay.getDay()==6) {                 
								nowRow = tbody_Calendar.insertRow();    // 새로운 행 추가
							}
							
							// 청소한 일자를 문자열로 받는다
							let cleanedDate = '';
							for(let i=0; i<result.dateList.length; i++){
								cleanedDate += result.dateList[i].date;
							}
							
							let date = nowDay.getFullYear() + "-" + leftPad(nowDay.getMonth()+1) + "-" + leftPad(nowDay.getDate());
							// 입력한 날짜가 청소한 일자에 포함되어 있다면
							if(cleanedDate.includes(date)){
								nowColumn.className = "cleanedDay";
								nowColumn.onclick = function(){ selectDate(this); }
							}
							else{
								nowColumn.className = "noCleanedDay";
							}
						}
					}
					
					// 이전달 버튼 클릭
					function prevCalendar() {
						nowMonth = new Date(nowMonth.getFullYear(), nowMonth.getMonth()-1, 1);   // 현재 달을 1 감소시키고 날짜는 1일로 설정
						getDateList(selectedCar_num.innerText.trim());
					}
					
					// 다음달 버튼 클릭
					function nextCalendar() {
						nowMonth = new Date(nowMonth.getFullYear(), nowMonth.getMonth()+1, 1);   // 현재 달을 1 증가시키고 날짜는 1일로 설정
						getDateList(selectedCar_num.innerText.trim());
					}
					
					// 날짜 선택
					function selectDate(nowColumn) {
						// 이미 선택되었던 날짜 class 제거
					    if (document.getElementsByClassName("selectedDay")[0]) {
					        document.getElementsByClassName("selectedDay")[0].classList.remove("selectedDay");
					    }
						// 새로 선택된 날짜 class 추가
					    nowColumn.classList.add("selectedDay");
					    
					    // 기존의 clean_o, clean_x, beginPoint, endPoint, course 레이어 삭제
					    maps.getLayers().getArray()
						  .filter(layer => layer.get('name')==='clean_o')
						  .forEach(layer => maps.removeLayer(layer));    
					    maps.getLayers().getArray()
						  .filter(layer => layer.get('name')==='clean_x')
						  .forEach(layer => maps.removeLayer(layer));
					    maps.getLayers().getArray()
						  .filter(layer => layer.get('name')==='start_point')
						  .forEach(layer => maps.removeLayer(layer));
					    maps.getLayers().getArray()
						  .filter(layer => layer.get('name')==='end_point')
						  .forEach(layer => maps.removeLayer(layer));
					    maps.getLayers().getArray()
						  .filter(layer => layer.get('name')==='clean_route')
						  .forEach(layer => maps.removeLayer(layer));

					    // 운행시간, 청소비율 구하기
					    let selectedDay = calYear.innerText + "-" + leftPad(calMonth.innerText) + "-" + leftPad(document.getElementsByClassName("selectedDay")[0].innerText);
					    getCleanTimeRatio(selectedDay, selectedCar_num.innerText.trim());
					    /* let intervalId = setInterval(function() {
					        let carNumber = selectedCar_num.innerText.trim();
					        getCleanTimeRatio(selectedDay, carNumber);
					    }, 5000); */
					}

					function getCleanTimeRatio(date, car_num){
						fetchGet("/cleanTimeRatio?date=" + date + "&car_num=" + car_num, showResult);
					}

					function showResult(result){
						
						var car_num = document.querySelector("#car").value;
						
						// 운행시간, 청소비율 띄우기
						clean_time.innerText = result.cleanTimeRatio.time;
						clean_ratio.innerText = result.cleanTimeRatio.ratio + "%";	
					    
						// clean_o, clean_x, beginPoint, endPoint, course 레이어 추가
						let selectedDay = calYear.innerText + "-" + leftPad(calMonth.innerText) + "-" + leftPad(document.getElementsByClassName("selectedDay")[0].innerText);
						var clean_o = new ol.layer.Tile({
							name : 'clean_o',
							 source: new ol.source.TileWMS({
						      url: 'http://182.222.169.37:8181/geoserver/opengis/wms',
						      params: {
						        'layers': 'opengis:Clean_O',
						        'tiled': 'true',
						        'VIEWPARAMS': 'date:'+selectedDay+';car_num:'+ car_num
						      },
						      serverType: 'geoserver'
						    })
					    });
					    
					    var clean_x = new ol.layer.Tile({
					    	name : 'clean_x',
							source: new ol.source.TileWMS({
						      url: 'http://182.222.169.37:8181/geoserver/opengis/wms',
						      params: {
						        'layers': 'opengis:Clean_X',
						        'tiled': 'true',
						        'VIEWPARAMS': 'date:'+selectedDay+';car_num:'+car_num
						      },
						      serverType: 'geoserver',
						      
						    })
					    });	
					    
					    var start_point = new ol.layer.Tile({
					    	name : 'start_point',
							source: new ol.source.TileWMS({
						      url: 'http://182.222.169.37:8181/geoserver/opengis/wms',
						      params: {
						        'layers': 'opengis:Start_Point',
						        'tiled': 'true',
						        'VIEWPARAMS': 'date:'+selectedDay
						      },
						      serverType: 'geoserver',
						      
						    })
					    }); 
					    
					    var end_point = new ol.layer.Tile({
					    	name : 'end_point',
							source: new ol.source.TileWMS({
						      url: 'http://182.222.169.37:8181/geoserver/opengis/wms',
						      params: {
						        'layers': 'opengis:End_Point',
						        'tiled': 'true',
						        'VIEWPARAMS': 'date:'+selectedDay
						      },
						      serverType: 'geoserver',
						      
						    })
					    }); 
					    
					    var clean_route = new ol.layer.Tile({
					    	name : 'clean_route',
							source: new ol.source.TileWMS({
						      url: 'http://182.222.169.37:8181/geoserver/opengis/wms',
						      params: {
						        'layers': 'opengis:Clean_Line',
						        'tiled': 'true',
						        'VIEWPARAMS': 'date:'+ selectedDay
						      },
						      serverType: 'geoserver',
						      
						    })
					    });
					    
					    
					  
					    
					    // 레이어 추가
					    maps.addLayer(clean_route);
					    
					    // 청소비율이 50% 이상이면 clean_o 레이어가 위로 가도록
						if(result.cleanTimeRatio.ratio>=50.00){
							maps.addLayer(clean_x);
						    maps.addLayer(clean_o);
						} 
						else{
							maps.addLayer(clean_o);
						    maps.addLayer(clean_x);
						}
						
					    maps.addLayer(start_point);
						maps.addLayer(end_point);
						
						console.log('===',result.center);
						
					    // 중심 좌표 이동
					    maps.getView().animate({
					        center: ol.proj.transform([parseFloat(result.center.lon), parseFloat(result.center.lat)], 'EPSG:4326', 'EPSG:900913'),
					        zoom: 14,
					        duration: 800
					    }); 
					}
					
					
					function leftPad(value) {
						if(value<10) {
							value = "0" + value;
							return value;
						}
						return value;
					}
					
					
					// 렛츠고 실시간 
					
					
               </script>
               
               <script>
               
            // 3초마다 해당 함수 호출하는 상수
   	        
   	      /*   const intervalId = setInterval(Socket, 3000);
   	      
	   		    $('#stopBtn').click(function(){
	   				
	   				clearInterval(intervalId);
			   				
			   			}); */
			   			
			   			var mapo = new ol.layer.Tile({
					    	name : 'clean_route',
							source: new ol.source.TileWMS({
						      url: 'http://182.222.169.37:8181/geoserver/opengis/wms',
						      params: {
						        'layers': 'opengis:mapo',
						        'tiled': 'true'
						      },
						      serverType: 'geoserver',
						      
						    })
					    });			   	
			   			
			   		function goLiveFunction(){
			   				
			            	   var obj = 'd';
			            	   fetchPost2('/goLive',obj, (map)=>{
			            		   console.log(' ====== ', map.list);
			            		   
			            		
				               		var live = new ol.layer.Tile({
										name : 'clean_o',
										 source: new ol.source.TileWMS({
									      url: 'http://182.222.169.37:8181/geoserver/opengis/wms',
									      params: {
									        'layers': 'opengis:Live',
									        'tiled': 'true',
									        'VIEWPARAMS': 'date:'+sysdate+';car_num:'+ car_num
									      },
									      serverType: 'geoserver'
									    })
								    });
				               		
				               
				               	maps.addLayer(live);
				               	
				               	 maps.getView().animate({
			    		            center: ol.proj.transform([126.944794, 37.556593], 'EPSG:4326', 'EPSG:900913'),
			    		            zoom: 18,
			    		            duration: 800
			            	   })
			            	   
			            	   
			        
			    		        
			               		});
			   				
			   				
			   			}
			   			
			   			
               
	               ////
	               // 라이브 시연
		               var goLive = document.querySelector('#goLive');
		               var goStop = document.querySelector('#goStop');
	               
	               goLive.addEventListener("click", () => {
	            	   	maps.addLayer(mapo);
	            	   
	            		 maps.getView().animate({
		    		            center: ol.proj.transform([126.944794, 37.556593], 'EPSG:4326', 'EPSG:900913'),
		    		            zoom: 16,
		    		            duration: 800
		            	   })
	            	   
		            	   goLive.style.display ="none";
	            		   goStop.style.display ="block";
	            	   
	            	   ////
	            	   const intervalId = setInterval(goLiveFunction, 5000);
	            	   
	            		 $('#goStop').click(function(){
	 						
	            			 goLive.style.display ="block";
		            		   goStop.style.display ="none";
		            		   maps.removeLayer(mapo);
	            			 
	 						clearInterval(intervalId);
	 						
	 					});
	            	   

	               	});   
	               
	              	
	               
	               
	               
	               
	            var sysdate;
	            var car_num;
	               
	               
               
               for(let i=0; i<${pList.size()}; i++){
   				
   				let selectedCar = document.querySelector("#btn_car_" + i);		
   				
   				selectedCar.addEventListener("click", function(){
               
               		// Date 객체 생성
	               let liveDate = new Date();
	               
	               let year = liveDate.getFullYear(); // year
	               let month = liveDate.getMonth() + 1; // month (January is 0, so we add 1)
	               let date = liveDate.getDate(); // date
	           
	               // Add leading zero to month and date if needed
	               if (month < 10) {
	                   month = '0' + month;
	               }
	           
	               if (date < 10) {
	                   date = '0' + date;
	               }
	           		
	               // 현재 날짜 저장
	              sysdate = year + '-' + month + '-' + date;
				
	               console.log('sysdate : ',sysdate);
	               
	               // car_num 읽어오기
	               car_num = document.querySelector("#car").value;
	               
	               console.log('car_num : ', car_num);
	               
	               ///goLive 버튼을 누르면 함수 실행??
					               
		            		   
	            		   
	               
	               
   				});
   			};
   				
               </script>
               
               <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.1/dist/js/bootstrap.bundle.min.js" integrity="sha384-HwwvtgBNo3bZJJLYd8oVXjrBZt8cqVSpeBNS5n7C8IVInixGAoxmnlMuBnhbgrkm" crossorigin="anonymous"></script>
</body>
</html>