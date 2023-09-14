<%@ page language="java" contentType="text/html; charset=UTF-8"
    pageEncoding="UTF-8"%>
<!DOCTYPE html>
<html>
<head>
<meta charset="UTF-8">
<title>통계페이지</title>

<style>
	
	   #chartContainer{
            margin: 0 auto;
            display: flex;
            width: 90%;
            
        }

        #sideTab{
            border: 1px solid;
            width: 10%;
        }

        #chartTab{
            border: 1px solid;
            width: 90%;
        }
        
</style>

</head>
<body>
		Hello, World !
		
		<div id="chartContainer">
        
	        <div id="sideTab">
	            <span>- 운행시간</span><br>
	            <span>- 청소비율</span>
	        </div>    
	
	        <div id="chartTab">
		            
		        <div>
		           <canvas id="myChart"></canvas>
				</div>
		        
		        
		        <div>
					<canvas id="dayRatio"></canvas>
		        </div>
	
	    	</div>
     
     	 </div>

      <!-- cnd 추가 -->
      <script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
      
      <script>
        const ctx = document.getElementById('myChart');
      
        new Chart(ctx, {
          type: 'line',
          data: {
            labels: ['1월', '2월', '3월', '4월', '5월', '6월', '7월', '8월', '9월', '10월', '11월', '12월'],
            datasets: [{
              label: '# 월별 운행시간',
              data: [3, 3, 3, 5, 2, 3, 10, 3, 15, 20, 30, 40],
              borderWidth: 1
            }]
          },
          options: {
            scales: {
              y: {
                beginAtZero: true
              }
            }
          }
        });
        
     // Sample data for driving and non-driving time in hours
        const drivingTime = 14; // Replace with your actual driving time
        const nonDrivingTime = 20; // Replace with your actual non-driving time

        // Create the data object for the donut chart
        const data = {
            labels: ['Driving', 'Non-Driving'],
            datasets: [{
                data: [drivingTime, nonDrivingTime],
                backgroundColor: ['rgba(255, 99, 132, 0.7)', 'rgba(54, 162, 235, 0.7)'],
                borderColor: ['rgba(255, 99, 132, 1)', 'rgba(54, 162, 235, 1)'],
                borderWidth: 1
            }]
        };

        // Create the donut chart
        const doughnut = document.getElementById('dayRatio').getContext('2d');
        const myDonutChart = new Chart(doughnut, {
            type: 'doughnut',
            data: data,
            options: {
                responsive: true,
                maintainAspectRatio: false,
                cutout: '50%', // Adjust the cutout size to control the thickness of the donut
                plugins: {
                    legend: {
                        position: 'top',
                        labels: {
                            boxWidth: 20,
                            fontStyle: 'bold'
                        }
                    }
                }
            }
        });
      </script>
</body>
</html>