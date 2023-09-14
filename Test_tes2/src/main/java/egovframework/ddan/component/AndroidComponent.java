package egovframework.ddan.component;

import java.util.List;

import javax.annotation.Resource;

import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Component;

import egovframework.ddan.mapper.TempMapper;
import egovframework.ddan.service.PointService;
import egovframework.ddan.vo.LocationVo;

@Component
public class AndroidComponent {
	
	
private static final long LOCATION_PROCESSING_INTERVAL = 10000;
	
	@Autowired
	TempMapper mapper;
	
	@Resource(name="pService")
	private PointService pService;
	
//	@Autowired 
//	PointService pService;
	 
	
	@Scheduled(fixedDelay = LOCATION_PROCESSING_INTERVAL)
	  public void processLocationData() { // 임시 테이블에서 데이터를 포인트 테이블로 이동하고 임시 테이블을 비우는 로직을 구현합니다.
		  
		List<LocationVo> list = mapper.getLocaList();
		  
		try {
			
			int res = pService.insertList(list);
			  
			  if(res > 0) {
		    		System.out.println("데이터 이동완료");
		    	}else {
		    		System.out.println("데이터 이동실패");
		    	}
			
		} catch (Exception e) {
			e.printStackTrace();
		}
		  

		  // 삭제
		  mapper.deleteTemt(); 
	  
	  }

}
