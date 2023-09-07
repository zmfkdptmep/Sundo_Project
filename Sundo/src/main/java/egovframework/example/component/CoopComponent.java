package egovframework.example.component;

import javax.annotation.Resource;
import javax.transaction.Transactional;

import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Component;

import egovframework.example.sample.service.CoopService;
import egovframework.example.sample.service.TempService;

@Component
public class CoopComponent {
	
	@Resource(name = "coopService")
	private CoopService service;
	
	@Resource(name = "tempService")
	private TempService service_temp;
	
	@Scheduled(fixedDelay = 60000)
	public void insertPoint() {
		
		int count = service_temp.countTemp();
		
		if(count>0) {
			
			int res = service.insertCoop();
			
			if(res>0) {
				
				int res2 = service_temp.deleteTemp();
				
				if(res2>0) {
					
					System.out.println("Temp DB 삭제 성공");
					
				} else {
					
					System.err.println("Temp DB 삭제 실패!!!!");
				}
				
			} else {
				
				System.err.println("스케줄러 DB 저장 실패!!!!");
				
			}
		}
	
	}

}
