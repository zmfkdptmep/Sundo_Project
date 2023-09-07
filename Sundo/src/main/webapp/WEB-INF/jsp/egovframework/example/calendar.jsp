<%@ page language="java" contentType="text/html; charset=UTF-8"
    pageEncoding="UTF-8"%>
<%@ taglib prefix='c' uri='http://java.sun.com/jsp/jstl/core' %>
<!DOCTYPE html>
<html lang="en">

<head>
  <meta charset="UTF-8">
  <meta http-equiv="X-UA-Compatible" content="IE=edge">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>달력 만들기</title>

  <link type="text/css" rel="stylesheet" href="/css/egovframework/calendar.css"/>

  <script src="https://ajax.googleapis.com/ajax/libs/jquery/1.12.1/jquery.min.js"></script>
  <script src="https://ajax.googleapis.com/ajax/libs/jqueryui/1.12.1/jquery-ui.min.js"></script>
  <script src="/js/calendar.js"></script>

</head>


<body>
	<div style="height:100px;">
		<span style="font-size:2.6em; font-weight:bold; color:#000000cf; position:absolute; left:3%; top:13%;">언제 떠날까요?</span>
		
	</div>
	<br><br><br>
	<hr style="border-top: 2.8px solid;color: #00000061;width:1150px;position:absolute;left:3%;">
	<hr style="border-top: 2.9px solid;color: #00000061;width:1150px;position:absolute;left:3%;top:620px;">


  <div class="calendar-wrap" style="padding-left:40px;">
    <div class="calendar-middle-wrap">
      <div class="cal_nav">
        <a href="javascript:;" class="nav-btn go-prev"></a>
        <span class="year-month start-year-month"></span>
        <a href="javascript:;" class="nav-btn go-next"></a>
      </div>
      <div class="cal_wrap">
        <div class="days">
          <div class="day">일</div>
          <div class="day">월</div>
          <div class="day">화</div>
          <div class="day">수</div>
          <div class="day">목</div>
          <div class="day">금</div>
          <div class="day">토</div>
        </div>
        <div class="dates start-calendar"></div>
      </div>
    </div>

    <div class="calendar-middle-wrap">
      <div class="cal_nav">
        <a href="javascript:;" class="nav-btn go-prev"></a>
        <span class="year-month last-year-month"></span>
        <a href="javascript:;" class="nav-btn go-next"></a>
      </div>
      <div class="cal_wrap">
        <div class="days">
          <div class="day">일</div>
          <div class="day">월</div>
          <div class="day">화</div>
          <div class="day">수</div>
          <div class="day">목</div>
          <div class="day">금</div>
          <div class="day">토</div>
        </div>
        <div class="dates last-calendar"></div>
      </div>
    </div>

    <div class="checkInOutInfo" style="height:400px;">
      <div>
        <p>
          <span style="padding-bottom:15px">체크인</span>
          <label id="check_in_day"></label>
        </p>
        <p class="space" style="color: #212529;font-size:2em;">~</p>
        <p>
          <span>체크아웃</span>
          <label id="check_out_day"></label>
        </p>
        <br><br>
        <p>
          <span>총 예약일</span>
          <label id="check_out_day" class="check_day_count"></label>          
        </p>
        
      </div>
    </div>
   <div id="buttonBtn">
    <form action="/reserved/day" method="get" onsubmit="return false;">
        <input type="hidden" id="reserved_day" name="reserved_day" value="">
        <input type="hidden" id="reserved_checkIn" name="reserved_checkIn" value="">
        <input type="hidden" id="reserved_checkOut" name="reserved_checkOut" value="">
		<input type="hidden" name="roomNo" value="${reserved.roomNo}">
		<input type="hidden" name="memberId" value="${reserved.memberId}">
		<input type="hidden" name="checkIn" value="${reserved.checkIn}">
		<input type="hidden" name="checkOut" value="${reserved.checkOut}">
		<input type="hidden" name="memberCount" value="${reserved.memberCount}">
		<input type="hidden" name="btnYN" value="${btnYN}">
		
		    <div style="position:absolute; top:720px; left:10%; width:1000px;" class="reserveBox">   
		    	<button id="reserveOr" style="padding-bottom:10px; background-color:white; border:0px; cursor: pointer;" type="submit" onclick="check(form)">예약하기</button><br><br>
		    	<button style="padding-bottom:10px; background-color:white; border:0px; cursor: pointer;" id="reload">초기화</button><br><br>
		    	<button style="padding-bottom:10px; background-color:white; border:0px; cursor: pointer;" id="back">메인으로</button><br><br>
		    </div>
		    	<button style="padding-bottom:10px; background-color:white; border:0px; cursor: pointer; position:absolute; top:11%; right:1%;" type="button" class="btn_close" onclick="btnClose()" id="closeBtn2">
		    	<img src="/images/closeBtn.JPG">
		    	</button>
    </form>
    </div>
    <script>
    	
    	
    
    
    	window.addEventListener('load', function(){
    		
    		var isReserve = '${param.roomNo}';
    		
    		if(isReserve!=''){
    			
    			$('#reserveOr').html('예약하기');
    		
    		} else {
    			
    			$('#reserveOr').html('검색하기');
    		}
    		
    		
    		/*
    		// 예약 페이지에선 닫기 버튼 안보여주기
    		if($('.modalOverlay2').attr('style') == null){
    			
    			$('#closeBtn2').attr('style', 'display:none;');
    			
    		}
    		*/
    		
    		if(checkInDate==''|| checkOutDate==''){
    			
    			$('.space').html('');
    			
    		}
    		
			
    		
    		
    		reload.addEventListener('click', function(e){
    			
    			e.preventDefault();
    			
    			//window.location.reload();
    			
    			// 달력 체크 인/아웃 값 초기화
    			$('.checkIn').find('.check_in_out_p').html('');
    			$('.checkOut').find('.check_in_out_p').html('');

    			$('.day').removeClass('checkIn');
    			$('.day').removeClass('checkOut');
    			$('.day').removeClass('selectDay');


    			$('.checkInOutInfo label').html('')



    			checkInDate = '';
    			checkOutDate = '';
				
    			// ~ 표시
    			$('.space').html('');
    		});
    		
    		back.addEventListener('click', function(e){
    			
    			e.preventDefault();
    			
    			location.href='/main';
    			
    		});
    		
    	});
    	
    	// 날짜 유효성 검사
    	function check(form){
    		
    		if(checkInDate==""){
    			
    				
    			alert('체크인 날짜를 선택해 주세요');
    			
    			return false;
    		}
    		
    		if(checkOutDate==""){
    			
    				
    			alert('체크아웃 날짜를 선택해 주세요');
    			
    			return false;
    		}
    		
    		form.submit();
    		
    			
    		
    	}
    	
    	var roomNo = '${roomInfo.ROOMNO}'; 
    	var list = '${disableList}';
    	
    	window.addEventListener('load', function(){
    		
    		var wherePage = '${param.stayLoc}';
    		var wherePage2 = '${param.btnYN}';
    		var wherePage3 = '${param.check_in}';
    		
    		if(wherePage!='' || wherePage2 !=''|| wherePage3 != ''){
    			
	    		$('.reserveBox').attr('style','position:absolute; top:750px; left:10%; width:1000px;');
	    		$('hr:eq(1)').attr('style','border-top: 2.9px solid;color: #00000061;width:1150px;position:absolute;left:3%;top: 680px;');
	    		
    		} else {
    			
    			
    			
    		}
    		
    	});
    	
    </script>
    
  </div>
</body>

</html>