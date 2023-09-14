package egovframework.ddan.controller;

import java.util.Random;

import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.context.annotation.Configuration;
import org.springframework.scheduling.annotation.EnableScheduling;
import org.springframework.stereotype.Controller;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.ResponseBody;

import com.google.gson.Gson;

import egovframework.ddan.service.TempService;
import egovframework.ddan.vo.LocationVo;

@Configuration
@EnableScheduling
@Controller
public class AndroidController {
		
	
	@Autowired
	TempService serive;
	
	
	  @PostMapping(value = "/android", consumes = "application/json")
	    public @ResponseBody void receiveLocation(@RequestBody LocationVo locationData) {
		  
		  System.out.println(locationData);
		  Random random = new Random();
		  
		  int noise = random.nextInt(51) + 80;
		  int rpm = random.nextInt(401) + 1300;
		  String car_num = "103하2414";
		  
		  	System.out.println("noise : " + noise);
		  	System.out.println("rpm : " + rpm);
	    	System.out.println("Received Location Data: " + locationData.getLatitude());
	    	System.out.println("Received Location Data: " + locationData.getLongitude());
	    
	    locationData.setNoise(noise);
	    locationData.setRpm(rpm);
	    locationData.setCar_num(car_num);
	    
	    	int res = serive.insert(locationData);
	    	
	    	if(res > 0) {
	    		System.out.println("위치저장 완료");
	    	}else {
	    		System.out.println("위치저장 실패");
	    	}
	    	
//	    	androidComponent.processLocationData();
	        
	    }
	
	
//	@PostMapping(value = "/android", consumes = "application/json")
//    public @ResponseBody void receiveLocation(@RequestBody String locationData) {
//        
//		System.out.println("실행~~~~~~~~~~~~~~~~~~~~~~~~~" + locationData);
//    	
//		Gson gson = new Gson();
//        LocationVo loca = gson.fromJson(locationData, LocationVo.class);
//        
//    	int res = serive.insert(loca);
//    	
//    	if(res > 0) {
//    		System.out.println("위치저장 완료");
//    	}else {
//    		System.out.println("위치저장 실패");
//    	}
//    	
//
//        
//    }
	
//	@PostMapping(value = "/android", consumes = "application/json")
//    public void receiveLocation(@RequestBody String locationData) {
//        System.out.println("Received Location Data: " + locationData);
//    } 
	
	 
	
}
