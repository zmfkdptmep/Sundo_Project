package egovframework.ddan.service;

import java.util.List;

import org.springframework.stereotype.Service;

import egovframework.ddan.vo.LocationVo;


@Service
public interface TempService {
	
	public int insert(LocationVo loca);
	
	public List<LocationVo> getLocaList();
	
	public int deleteTemt();
		
}
