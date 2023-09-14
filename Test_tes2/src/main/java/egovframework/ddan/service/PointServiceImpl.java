package egovframework.ddan.service;

import java.util.HashMap;
import java.util.List;
import java.util.Map;

import javax.annotation.Resource;

import org.egovframe.rte.fdl.cmmn.EgovAbstractServiceImpl;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;

import egovframework.ddan.mapper.PointMapper;
import egovframework.ddan.vo.CarVo;
import egovframework.ddan.vo.CleanVo;
import egovframework.ddan.vo.CsvVO;
import egovframework.ddan.vo.LocationVo;
import egovframework.ddan.vo.PointsVo;

@Service("pService")
public class PointServiceImpl extends EgovAbstractServiceImpl implements PointService{

	@Resource(name="pMapper")
	private PointMapper pMapper;
	
	@Override
	public int insertList(List<LocationVo> list) {
		
		int insertRes = 0;
		
		System.out.println("원본 리스트 출력 =================" + list);
		
		for(LocationVo lolist : list) {
				System.out.println("리스트 반복 출력 ~~~~~~~~~~~~~" + lolist.toString());
			int res = pMapper.insertList(lolist);
		
			if(res > 0) {
				insertRes++;
			}
		}
		
		return insertRes;
	}

	@Override
	public List<PointsVo> getCarList() {
		// TODO Auto-generated method stub
		return pMapper.getCarList();
	}

	@Override
	public CleanVo getCleanData(PointsVo points) {
		// TODO Auto-generated method stub
		return pMapper.getCleanData(points);
	}

	@Override
	public int insertCsv(CsvVO vo) {
		// TODO Auto-generated method stub
		return pMapper.insertCsv(vo);
	}

	
	 @Override public List<CleanVo> getStaics(PointsVo points) { 
	  return pMapper.getStaics(points); 
	  
	 }

	@Override
	public CleanVo monthData() {
		// TODO Auto-generated method stub
		return pMapper.monthData();
	}

	@Override
	public Map<String, Object> getDateList(String car_num) {
		Map<String, Object> map = new HashMap<String, Object>();
		map.put("dateList", pMapper.getDateList(car_num));
		return map;		
	}

	@Override
	public Map<String, Object> getCleanTimeRatio(String date, String car_num) {
		Map<String, Object> map = new HashMap<String, Object>();
		map.put("cleanTimeRatio", pMapper.getCleanTimeRatio(date, car_num));
		
		// 청소 시작점, 끝점 구하기
		map.put("center", pMapper.getCenter(date, car_num));
		return map;
	}

	@Override
	public List<PointsVo> goLive() {
		// TODO Auto-generated method stub
		return pMapper.goLive();
	}

	@Override
	public MemberVo login(MemberVo member) {
		// TODO Auto-generated method stub
		return pMapper.login(member);
	}

	@Override
	public int addCar(CarVo carVo) {
		// TODO Auto-generated method stub
		return pMapper.addCar(carVo);
	}
	
	
	

}
