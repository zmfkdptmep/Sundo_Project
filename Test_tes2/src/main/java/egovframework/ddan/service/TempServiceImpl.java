package egovframework.ddan.service;

import java.util.List;

import org.egovframe.rte.fdl.cmmn.EgovAbstractServiceImpl;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;

import egovframework.ddan.mapper.TempMapper;
import egovframework.ddan.vo.LocationVo;

@Service
public class TempServiceImpl extends EgovAbstractServiceImpl implements TempService {

	@Autowired
	TempMapper mapper;
	
	@Override
	public int insert(LocationVo locationData) {
		// TODO Auto-generated method stub
		return mapper.insert(locationData);
	}

	@Override
	public List<LocationVo> getLocaList() {
		// TODO Auto-generated method stub
		return mapper.getLocaList();
	}

	@Override
	public int deleteTemt() {
		// TODO Auto-generated method stub
		return mapper.deleteTemt();
	}

}
