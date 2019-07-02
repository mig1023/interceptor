package VCS::Interceptor;
use strict;

use VCS::Config;
use LWP;
use HTTP::Request;
use Digest::MD5  qw(md5_hex);
use Data::Dumper;
use Encode qw(decode encode);

sub protocol_pass { return ''; };

sub protocol_port { return 80; };

sub new
# //////////////////////////////////////////////////
{
	my ( $class, $pclass, $vars ) = @_;
	
	my $self = bless {}, $pclass;
	
	$self->{ 'VCS::Vars' } = $vars;
	
	return $self;
}

sub send_connection_signal
# //////////////////////////////////////////////////
{
	my ( $self, $interceptor ) = @_;

	my $vars = $self->{ 'VCS::Vars' };
	
	my $request = '<?xml version="1.0" encoding="UTF-8"?>' . 
		'<toCashbox>' . 
			'<CheckConnection>MakeBeep</CheckConnection>' .
			'<Info><Cashier>' . $vars->get_session->{'login'} . '</Cashier>' .
			'<Region>false</Region></Info>' .
		'</toCashbox>';

	return send_request( $vars, $request, $interceptor );
}

sub md5_crc_with_secret_code
# //////////////////////////////////////////////////
{
	my ( $bytecode, $not_ord ) = @_;
	
	if ( $not_ord ) {
	
		$bytecode .= protocol_pass();
	}
	else {
		$bytecode .= ord( $_ ) . " " for split( //, protocol_pass() );
	}

	return Digest::MD5->new->add( $bytecode )->hexdigest;
}

sub md5_filecrc_with_secret_code
# //////////////////////////////////////////////////
{
	my $file_check = shift;
	
	my $md5file = Digest::MD5->new;
	
	$md5file->addfile( $file_check );
	
	my $bytecode .= ord( $_ ) . " " for split( //, protocol_pass() );
	
	$md5file->add( $bytecode );
	
	return $md5file->hexdigest;
}	

sub send_docpack
# //////////////////////////////////////////////////
{
	my ( $self, $docid, $ptype, $summ, $data, $login, $pass, $callback, $r, $sh_return,
		$sms, $email, $region ) = @_;

	my $vars = $self->{ 'VCS::Vars' };
	
	my $interceptor = $vars->get_session->{ interceptor };
	
	my $region_interceptor = undef;
	
	$region_interceptor = $region->{ interceptor }
		if ( ref( $region ) eq 'HASH' and $region->{ type } eq 'region_cashbox' );	
	
	if ( !$data ) {
	
		my $docobj = VCS::Docs::docs->new( 'VCS::Docs::docs', $vars );
		
		my $error = $docobj->getDocData( \$data, $docid, 'individuals' );
		
		return ( "ERR3", "Ошибка доступа к данным договора" ) unless $error eq '';
	}	
	
	return ( "ERR3", "Договор юрлица не может быть оплачен" ) if $data->{ jurid };
	
	$login = $vars->get_session->{'login'} unless $login;
	
	if ( !$callback && !$pass ) {
	
		return ( "ERR3", "Неверные настройки данных подключения" )
			unless $interceptor or $region_interceptor;
		
		$pass = $vars->db->sel1("
			SELECT CashPassword FROM Cashboxes_interceptors WHERE ID = ?",
			( $region_interceptor ? $region_interceptor : $interceptor )
		);
		
	}
	elsif ( !$pass ) {
	
		$pass = $vars->db->sel1("
			SELECT CashPassword FROM Cashboxes_interceptors WHERE InterceptorIP = ?", $callback
		);
	}
	
	return ( "ERR3", "Неверный кассовый пароль в настройках" ) unless $pass;
	
	$data->{ email } = ( $email ? $email : '' );
	
	$data->{ sms_mobile } = ( $sms ? $sms : '' );
	
	my ( $services, $services_fail ) = doc_services( $self, $data, $ptype, $summ, $login, $pass, $callback, $r, $sh_return, $region_interceptor );

	my $request = xml_create( $services->{ services }, $services->{ info } );

	my $resp = send_request( $vars, $request, $region_interceptor, $callback );
	
	if ( $vars->get_session->{ interceptor } ) {
	
		$vars->db->query("
			UPDATE Cashboxes_interceptors SET LastUse = now(), LastResponse = ? WHERE ID = ?", {},
			$resp, $vars->get_session->{ interceptor }
		);
	}
	else {
		$vars->db->query("
			UPDATE Cashboxes_interceptors SET LastUse = now(), LastResponse = ? WHERE InterceptorIP = ?", {},
			$resp, $callback
		);
	}
	
	my ( $code, $desc ) = split( /:/, $resp );
	
	return $code, $desc, $services_fail;
}

sub xml_create
# //////////////////////////////////////////////////
{
	my ( $services, $info ) = @_;
	
	my ( $md5line, $bytecode ) = ( '', '' );
	
	my $xml = '<Services>';
	
	for my $service ( keys %$services ) {
	
		$xml .= '<Service>';
	
		for my $field ( sort { $a cmp $b } keys %{ $services->{ $service } } ) {
		
			$xml .= "<$field>" . $services->{ $service }->{ $field } . "</$field>";
			
			$md5line .= $services->{ $service }->{ $field };
		}
		
		$xml .= '</Service>';
	}
	
	$xml .= '</Services>';
	
	$xml .= '<Info>';
	
	for ( sort { $a cmp $b } keys %$info ) {
	
		$xml .= "<$_>" . $info->{ $_ } . "</$_>";
		
		$md5line .= $info->{ $_ };
	}
	
	$xml .= '</Info>';
	
	my $currentDate = get_date();
	
	$md5line .= $currentDate;
	
	Encode::from_to( $md5line, 'utf-8', 'windows-1251' );

	$bytecode .= ord( $_ ) . " " for split( //, $md5line );

	my $md5 = md5_crc_with_secret_code( $bytecode );
	
	$xml =  '<?xml version="1.0" encoding="UTF-8"?>' . 
		'<toCashbox>' . 
			$xml .
			'<Control>' .
				'<CRC>' . $md5 . '</CRC>' . 
				'<Date>' . $currentDate . '</Date>' .
			'</Control>' .
		'</toCashbox>';

	return $xml;
}

sub send_request
# //////////////////////////////////////////////////
{
	my ( $vars, $line, $interceptor, $callback ) = @_;
	
	my $serv;

	my $port = protocol_port();
	
	if ( !$callback ) {
	
		$interceptor = $vars->get_session->{ interceptor } unless $interceptor;

		$serv = $vars->db->sel1("
			SELECT InterceptorIP FROM Cashboxes_interceptors WHERE ID = ?",
			$interceptor
		);
	}
	else {
		$serv = $callback;
	}
	
	return "ERR4:Не установлена кассовая интеграция" unless $serv =~ /^([0-9]{1,3}[\.]){3}[0-9]{1,3}$/;

	my $ua = LWP::UserAgent->new;
	
	$ua->agent( 'Mozilla/4.0 (compatible; MSIE 6.0; X11; Linux i686; en) Opera 7.60' );

	my $request = HTTP::Request->new( GET => 'http://'."$serv:$port".'/?message='.$line.';' );

	my $response = $ua->request( $request );

	return "ERR1:Нет связи с кассовой программой (она не запущена?)" if $response->{ _rc } != 200;
	
	return $response->{ _content };
}

sub get_date
# //////////////////////////////////////////////////
{
	my ( $sec, $min, $hour, $mday, $mon, $year, $wday, $yday, $isdst ) = localtime( time );
	
	$year += 1900;
	
	$mon += 1;
	
	for ( $sec, $min, $hour, $mday, $mon, $year ) { 
	
		$_ = '0' . $_ if $_ < 10;
	};
	
	return "$year-$mon-$mday $hour:$min:$sec";
}

sub get_all_add_services
# //////////////////////////////////////////////////
{
	my ( $self, $vars, $data, $callback ) = @_;
	
	my $services = [];

	my $serv_price = $vars->db->selallkeys("
		SELECT ServiceID, Price FROM ServicesPriceRates WHERE PriceRateID = ?",
		$data->{ rate }
	);
	
	my %serv_price = map { $_->{ ServiceID } => $_->{ Price } } @$serv_price;

	if ( !$callback ) {

		$services = $vars->db->selallkeys("
			SELECT Services.ID as ServiceID, Services.Name, DocPackService.ServiceID,
			ServiceFields.ValueType, ServiceFieldValuesINT.Value
			FROM DocPackService
			JOIN Services ON DocPackService.ServiceID = Services.ID
			JOIN ServiceFields ON DocPackService.ServiceID = ServiceFields.ServiceID
			JOIN ServiceFieldValuesINT ON ServiceFieldValuesINT.DocPackServiceID = DocPackService.ID
			WHERE PackID = ?
			GROUP BY DocPackServiceID",
			$data->{ docid }
		);
	}
	else {
		my $serv_names = $vars->db->selallkeys( "SELECT ID, Name FROM Services" );
		
		my %services_name = map { $_->{ ID } => $_->{ Name } } @$serv_names;

		for ( sort { $a <=> $b } keys %services_name ) {
		
			if ( $data->{ "srv$_" } ) {

				my $value = ( $_ == 1 ? $serv_price{ $_ } : $data->{ "srv$_" } ) || 0;
				
				push @$services, {
				
					ServiceID 	=> $_,
					Name 		=> $services_name{ $_ },
					Value 		=> $value,
					ValueType 	=> ( $_ == 1 ? 1 : 2 ),
				};
			}
		}
	}

	my $serv_list = {};
	
	for ( @$services ) {
	
		if ( $_->{ ValueType } == 1 ) {
			
			$serv_list->{ "service" . $_->{ ServiceID } } = {
			
				Name		=> $_->{ Name },
				Quantity	=> 1,
				Price		=> $_->{ Value },
				VAT 		=> 1,
				Department	=> 1,
			};
		}
		elsif ( $_->{ ValueType } == 2 ) {
		
			my $price = $serv_price{ $_->{ ServiceID } } || 0;
		
			$serv_list->{ "service" . $_->{ ServiceID } } = {
			
				Name		=> $_->{ Name },
				Quantity	=> $_->{ Value },
				Price		=> $price,
				VAT 		=> 1,
				Department	=> 1,
			};
		};
	}

	return $serv_list;
}

sub get_service_code
# //////////////////////////////////////////////////
{
	my ( $self, $serv, $center, $urgance, $ord ) = @_;
	
	my $country = '(' . 'ITA';
		
	my $center_id = {
		1	=> '00',
		2	=> '01',
		9	=> '02',
		8	=> '03',
		3	=> '04',
		5	=> '05',
		7	=> '06',
		6	=> '07',
		4	=> '08',
		12	=> '10',
		13	=> '11',
		14	=> '12',
		15	=> '14',
		16	=> '15',
		18	=> '16',
		17	=> '17',
		19	=> '18',
		20	=> '19',
		22	=> '20',
		21	=> '21',
		23	=> '22',
		25	=> '25',
		24	=> '26',
		26	=> '27',
		31	=> '91',
		32	=> '91',
		40	=> '92',
		41	=> '92',
		44	=> '92',
		45	=> '92',
		
		11	=> '09',
		27	=> '23',
		28	=> '23',
		29	=> '09',
		30	=> '09',
		33	=> '30',
		37	=> '09',
		38	=> '29',
		43	=> '09',
	};
	
	my $serv_group = {
		'shipping'	=> '501',
		'sms'		=> '300',
		'tran'		=> '000',
		'xerox'		=> '400',
		'ank'		=> '502',
		'print'		=> '503',
		'photo'		=> '504',
		'vip'		=> '505',
		'service1' 	=> '506',
		'service2' 	=> '704',
		'service3' 	=> '000',
		'service4' 	=> '705',
		'service5' 	=> '511',
		'service6' 	=> '512',
		'service7' 	=> '513',
		'service8' 	=> '514',
		'service9' 	=> '515',
		'service10' 	=> '000',
	};
	
	if ( $serv eq 'visa' ) {
	
		my ( $pay_type, $agent ) = ( 1, 1 );
		
		my $urgance_code = ( $urgance ? '1' : '0' );
		
		return $country . $center_id->{ $center } . $pay_type . $urgance_code . $agent . ') ';
	}
	else {
	
		return $country . $center_id->{ $center } . $serv_group->{ $serv } . ') ';
	}
}

sub doc_services
# //////////////////////////////////////////////////
{
	my ( $self, $data, $ptype, $summ, $login, $pass, $callback, $reception, $sh_return, $region_cashbox ) = @_;

	my $vars = $self->{'VCS::Vars'};

	my ( $cntres, $cntnres, $cntncon, $cntage, $smscnt, $shcnt, $shrows, $shind, $indexes, $dhlsum, $inssum, $inscnt ) = 
		( 0, 0, 0, 0, 0, 0, {}, '', {}, 0, 0, 0 );

	if ( $data->{ shipping } == 1 ) {

		$dhlsum = $data->{ shipsum };
		
		$shcnt = $data->{ shipping };
	}

	my ( $apcnt, $astr, $bankid, $prevbank ) = ( 0, '', '', 0 );
	
	for my $ak ( @{ $data->{ applicants } } ) {

		next if $ak->{ Status } == 7;

		$apcnt += 1 unless $data->{ direct_docpack } and $ak->{ Status } != 1;
		
		if ( !$data->{ direct_docpack } or $ak->{ direct_concil } ) {
		
			if ( $ak->{ Concil } ) {
			
				$cntncon += 1;
			}
			else {
				if ( $ak->{ AgeCatA } ) {
				
					$cntage += 1;
				}
				else {
					if ( $ak->{ iNRes } ) {
					
						$cntnres += 1;
					}
					else {					
						$cntres += 1;
					}
				}
			}
		}

		if ( ( $data->{ sms_status } == 2 ) && ( $ak->{ MobileNums } ne '' ) ) {
		
			$smscnt += 1;
		}
		
		if ( ( $data->{ shipping } == 2 ) && ( $ak->{ ShipAddress } ne '' ) ) {
		
			$shcnt += 1;
			
			$dhlsum += $ak->{ RTShipSum };
		}
	}
	
	my ( $agesfree, $ages ) = $vars->db->sel1("
		SELECT AgesFree, Ages FROM PriceRate WHERE ID = ?",
		$data->{ rate }
	);
	
	my $main_bankid = '';
	
	my $all_bankid = $vars->db->selallkeys("
		SELECT PackID, BankID FROM DocPackInfo WHERE PackID=?",
		$data->{ docid }
	);

	$main_bankid .= $_->{ BankID } . ',' for ( @$all_bankid );
	$main_bankid =~ s/,$//;
	
	$main_bankid = '' if $main_bankid =~ /^[^\d,]+$/;

	my $concil_payment_date = $vars->get_system->now_date();

	$concil_payment_date = "$3-$2-$1" if $data->{ concilPaymentDate } =~ /(\d{2})\.(\d{2})\.(\d{4})/;

	$concil_payment_date = $data->{ concilPaymentDate } if $data->{ concilPaymentDate } =~ /(\d{4})\-(\d{2})\-(\d{2})/;

	my $prices = $vars->admfunc->getPrices( $vars, $data->{ rate }, $data->{ vtype }, $concil_payment_date );
	
	my $insurance_rgs = $data->{ insurance_manual_service_RGS } || '0.00';
	
	my $insurance_kl = $data->{ insurance_manual_service_KL } || '0.00';
	
	my $shsum = sprintf( "%.2f", ( $data->{ newdhl } ? $dhlsum : $prices->{ shipping } * $shcnt ) );
	
	$smscnt = 1 if $data->{ sms_status } == 1;
	
	my $vprice = ( $data->{ urgent } ? $prices->{ 'urgent' } : $prices->{ 'visa' } );
	
	my $special_department = ( $reception ? 3 : 1 );
	
	my $servsums = {
		shipping => {
			Name		=> 'Услуги по доставке документов на дом',
			Quantity	=> $shcnt,
			Price		=> sprintf( "%.2f", $shsum ),
			VAT		=> 1,
			Department	=> 1,
			Shipping	=> 1,
			ReturnShipping	=> ( $sh_return ? 1 : 0 ),
		},
		sms => {
			Name		=> 'Услуги по оповещению (СМС сообщение)',
			Quantity	=> $smscnt,
			Price		=> sprintf( "%.2f", $prices->{ sms } ),
			VAT		=> 1,
			Department	=> 1,
		},
		tran => {
			Name		=> 'Услуги по переводу документов',
			Quantity	=> $data->{ transum },
			Price		=> sprintf( "%.2f", $prices->{ tran } ),
			VAT		=> 1,
			Department	=> 1,
		},
		xerox => {
			Name		=> 'Услуги по копированию документов',
			Quantity	=> $data->{ xerox },
			Price		=> sprintf( "%.2f", $prices->{ xerox } ),
			VAT		=> 1,
			Department	=> $special_department,
			ReceptionID	=> 1,
		},
		visa => {
			Name		=> 'Услуги по оформлению документов',
			Quantity	=> $apcnt,
			Price		=> sprintf( "%.2f", $vprice ),
			VAT		=> 1,
			Department	=> 1,
		},
		ank => {
			Name		=> 'Услуги по заполнению и распечатке анкеты' . ( $reception ? '' : ' заявителя' ),
			Quantity	=> $data->{ anketasrv },
			Price		=> sprintf( "%.2f", $prices->{ anketasrv } ),
			VAT		=> 1,
			Department	=> $special_department,
			ReceptionID	=> 2,
		},
		print => {
			Name		=> 'Услуги по распечатке документов',
			Quantity	=> $data->{ printsrv },
			Price		=> sprintf( "%.2f", $prices->{ printsrv } ),
			VAT		=> 1,
			Department	=> $special_department,
			ReceptionID	=> 3,
		},
		photo => {
			Name		=> 'Услуги фотосъемке и изготовлению фото',
			Quantity	=> $data->{ photosrv },
			Price		=> sprintf( "%.2f", $prices->{ photosrv } ),
			VAT		=> 1,
			Department	=> $special_department,
			ReceptionID	=> 4,
		},
		vip => {
			Name		=> 'Услуги по ВИП обслуживанию',
			Quantity	=> $data->{ vipsrv },
			Price		=> sprintf( "%.2f", $prices->{ vipsrv } ),
			VAT		=> 1,
			Department	=> 1,
		},
		insurance_rgs => {
			Name		=> 'Страхование',
			Quantity	=> ( $data->{ insurance_manual_service_RGS } ? 1 : 0 ),
			Price		=> sprintf( "%.2f", $insurance_rgs ),
			VAT		=> 0,
			Department	=> 4,
			WithoutServCode	=> 1,
		},
		insurance_klf => {
			Name		=> 'Страхование',
			Quantity	=> ( $data->{ insurance_manual_service_KL } ? 1 : 0 ),
			Price		=> sprintf( "%.2f", $insurance_kl ),
			VAT		=> 0,
			Department	=> 5,
			WithoutServCode	=> 1,
		},
	};
	
	my $concil_price = {};
	my $u = ( $data->{ urgent } ? 'u' : '' );
	
	concil_add_price( $concil_price, $prices, "concilr$u", $cntres ) if $data->{ vcat } eq 'C';
	concil_add_price( $concil_price, $prices, "conciln$u", $cntnres );
	concil_add_price( $concil_price, $prices, "concilr$u" . '_' . $ages, $cntage );
	concil_add_price( $concil_price, $prices, "concilr$u", $cntres ) if $data->{ vcat } eq 'D';

	my $consil_index = 0;
	
	my $concil_price_to_name = {
		35  => 'C01',
		50  => 'D05',
		60  => 'C03',
		70  => 'C02',
		116 => 'D04',
	};
	
	for ( keys %$concil_price ) {
	
		$consil_index += 1;
		
		my $concil_name = $concil_price_to_name->{ $concil_price->{ $_ }->{ type } } || '';
	
		$servsums->{ "concil$consil_index" } = {
			Name		=> "Консульский сбор $concil_name",
			Quantity	=> $concil_price->{ $_ }->{ cnt },
			Price		=> $_,
			VAT		=> 0,
			Department	=> 2,
			WithoutServCode	=> 1,
		};
	}
	
	my $serv_hash = get_all_add_services( $self, $vars, $data, $callback );
	
	for ( keys %$serv_hash ) {
	
		$servsums->{ $_ } = $serv_hash->{ $_ };
	}

	my ( $total, $mandocpack_failserv, $ord ) = ( 0, undef, 1 );
	
	for my $serv ( keys %$servsums ) {

		$servsums->{ $serv }->{ Name } =
			get_service_code( $self, $serv, $data->{ center }, $data->{ urgent }, $ord ) . $servsums->{ $serv }->{ Name }
				unless $servsums->{ $serv }->{ WithoutServCode };

		$mandocpack_failserv .= ( $mandocpack_failserv ? ', ' : '' ) . $servsums->{ $serv }->{ Name }
			if $servsums->{ $serv }->{ Quantity }
				and (
					$servsums->{ $serv }->{ Price } eq '0.00'
					or
					!$servsums->{ $serv }->{ Price }
				);

		if ( 
			(
				!$servsums->{ $serv }->{ Quantity } 
				or
				$servsums->{ $serv }->{ Price } eq '0.00' or !$servsums->{ $serv }->{ Price }
			) or (
				$sh_return && !$servsums->{ $serv }->{ Shipping }
			) or (
				( $serv eq 'shipping' ) and $data->{ reject } and $region_cashbox
			)
		) {
		
			delete $servsums->{ $serv };
		}
		else {
			$ord += 1;
		
			$total += $servsums->{ $serv }->{ Price } * $servsums->{ $serv }->{ Quantity };
		}
	}

	my $info = {
		AgrNumber => $data->{ docnum },
		MainBankID => $main_bankid,
		Cashier => $login,
		Region => ( $region_cashbox ? "true" : "false" ),
		CashierPass => $pass,
		MoneyType => $ptype,
		Total => $total,
		Money => $summ,
		RequestOnly => ( $callback ? '1' : '0' ),
		EMail => $data->{ email },
		Mobile => $data->{ sms_mobile },
	};

	return { services => $servsums, info => $info }, $mandocpack_failserv;
}

sub concil_add_price
# //////////////////////////////////////////////////
{
	my ( $concil_price, $prices, $price_type, $cnt ) = @_;

	my $current_cnt;
	
	if ( ref( $concil_price->{ sprintf( '%.2f', $prices->{ $price_type } ) } ) eq 'HASH' ) {
	
		$current_cnt = $concil_price->{ sprintf( '%.2f', $prices->{ $price_type } ) }->{ cnt } || 0;
	}
	else {
		$current_cnt = 0;
		$concil_price->{ sprintf( '%.2f', $prices->{ $price_type } ) } = {};
	}
	
	$concil_price->{ sprintf( '%.2f', $prices->{ $price_type } ) }->{ cnt } = $current_cnt + $cnt;
	
	$concil_price->{ sprintf( '%.2f', $prices->{ $price_type } ) }->{ type } = $prices->{ 'o' . $price_type };
	
	return $concil_price;
}

sub cashbox_error
# //////////////////////////////////////////////////
{
	my ( $self, $task, $id, $template, $slist, $authip, $clientip ) = @_;

	my $vars = $self->{'VCS::Vars'};
	
	my $param = {};
	
	$param->{ $_ } = ( $vars->getparam( $_ ) || 0 ) for ( 'login', 'error', 'ip', 'agr' );
	
	$param->{ agr } =~ s/[^\d]+//g;
	
	my ( $cashbox_id, $cashbox_name ) = $vars->db->sel1("
		SELECT ID, Name FROM Cashboxes_interceptors WHERE InterceptorIP = ?", $param->{ ip }
	);
	
	my $docpack_id = 0;
	
	if ( $param->{ agr } ) {
	
		$docpack_id = $vars->db->sel1("
			SELECT ID FROM DocPack WHERE AgreementNo = ?", $param->{ agr }
		) || 0;
	}
	
	if ( !$cashbox_id ) {
	
		my $error = "ошибочный запрос с IP = " . $param->{ ip } . " ($clientip) с текстом: " .  $param->{ error };
	
		$vars->db->query("
			INSERT INTO Cashboxes_errors (CashboxID, Login, DocPackID, ErrorDate, Error) VALUES (null, ?, null, now(), ?)", {},
			$param->{ login }, $error
		);
	}
	else {
		$vars->db->query("
			INSERT INTO Cashboxes_errors (Cashbox, CashboxID, Login, DocPackID, ErrorDate, Error) VALUES (?, ?, ?, ?, now(), ?)", {},
			$cashbox_name, $cashbox_id, $param->{ login }, $docpack_id, $param->{ error }
		);
	}
		
	return cash_box_output( $self, "OK|Информация получена" );
}

sub cash_box_output_error_check
# //////////////////////////////////////////////////
{
	my ( $self, $code, $desc, $docid ) = @_;
	
	my $vars = $self->{'VCS::Vars'};

	if ( $code ne 'OK' ) {
	
		my $err_type = "(неизвестная ошибка)";
	
		$err_type = "(ошибка кассовой программы)" if $code =~ /^ERR1$/;
		$err_type = "(ошибка кассы)" if $code =~ /^ERR2$/;
		$err_type = "(ошибка настроек)" if $code =~ /^ERR(3|4)$/;
		
		$vars->db->query("
			INSERT INTO Cashboxes_errors (CashboxID, Login, DocPackID, ErrorDate, Error) VALUES (?, ?, ?, now(), ?)", {},
			$vars->get_session->{ interceptor }, $vars->get_session->{'login'}, $docid, "$err_type $desc"
		);
	
		return cash_box_output( $self, "ERROR|$err_type $desc" );
	}
	
	return cash_box_output( $self, "OK|$desc" );
}

sub cash_box_ship_return
# //////////////////////////////////////////////////
{
	my $self = shift;
	
	my $vars = $self->{'VCS::Vars'};

	my $docid = $vars->getparam( 'docid' ) || 0;
	
	return cash_box_output( $self, "ERROR|Ошибка параметров" )
		unless $docid;
	
	my ( $ptype, $summ ) = $vars->db->sel1("
		SELECT PType, TShipSum FROM DocPack WHERE ID = ?",
		$docid
	);
	
	my ( $code, $desc, undef ) = send_docpack( $self, $docid, $ptype, $summ, undef, undef, undef, undef, undef, 'sh_return' );
	
	cash_box_output_error_check( $self, $code, $desc, $docid );
}

sub cash_box
# //////////////////////////////////////////////////
{
	my $self = shift;
	
	my $vars = $self->{'VCS::Vars'};
	
	my $param = {};
	
	$param->{ $_ } = ( $vars->getparam( $_ ) || 0 )
		for ( 'docid', 'ptype', 'summ', 'sms', 'email', 'need_sms', 'need_email' );
	
	$param->{ $_ } =~ s/[^0-9]//g for ( 'docid', 'ptype', 'sms', 'need_sms', 'need_email' );
	
	$param->{ email } =~ s/A-Za-z0-9\@\-\_\.]//g;
	
	$param->{ summ } =~ s/,/./g if $param->{ summ } =~ /,/;
	
	return cash_box_output( $self, "ERROR|Недопустимые символы в поле суммы" )
		if $param->{ summ } =~ /[^0-9\.]/;
	
	return cash_box_output( $self, "ERROR|Ошибка параметров" )
		if !$param->{ docid } or ( $param->{ ptype } != 1 and $param->{ ptype } != 2 );
	
	return cash_box_output( $self, "ERROR|Не введена сумма оплаты" )
		if !$param->{ summ } and $param->{ ptype } == 1;

	return cash_box_output( $self, "ERROR|Не указан номер для SMS" )
		if !$param->{ sms } and $param->{ need_sms };
		
	return cash_box_output( $self, "ERROR|Не указан адрес электронной почты" )
		if !$param->{ email } and $param->{ need_email };	
	
	my ( $code, $desc, undef ) = send_docpack( $self, $param->{ docid }, $param->{ ptype }, $param->{ summ },
		undef, undef, undef, undef, undef, undef, $param->{ sms }, $param->{ email } );
	
	cash_box_output_error_check( $self, $code, $desc, $param->{ docid } );
}

sub cash_box_region
# //////////////////////////////////////////////////
{
	my ( $self, $data, $docid, $region_id, $email ) = @_;
	
	my $region = {
		type => 'region_cashbox',
		interceptor => ( $region_id == 1 ? 21 : 22 ),
	};
	
	my ( $code, $desc, undef ) = send_docpack(
		$self, $docid, 2, undef, $data, undef, undef,
		undef, undef, undef, undef, $email, $region
	);
	
	return ( $code, $desc );
}

sub cash_box_auth
# //////////////////////////////////////////////////
{
	my ( $self, $task, $id, $template, $slist, $authip, $clientip ) = @_;
	
	my $vars = $self->{'VCS::Vars'};

	my $param = {};
	
	$param->{ $_ } = ( $vars->getparam( $_ ) || '' ) for ( 'login', 'p', 'ip', 'v' );

	return cash_box_output( $self, "ERROR|Укажите данные для авторизации" ) if !$param->{ login } or !$param->{ p };
	
	my ( $login, $name, $surname, $secname ) = $vars->db->sel1("
		SELECT Login, UserName, UserLName, UserSName
		FROM Users
		WHERE Login = ? AND Pass = ? AND
		(RoleID = 8 OR RoleID = 5 OR RoleID = 2 OR RoleID = 23 OR RoleID = 6 OR RoleID = 39)
		AND Locked = 0",
		$param->{ login }, $param->{ p }
	);

	return cash_box_output( $self, "ERROR|Неудачная авторизация в VMS" ) unless $login;
	
	return cash_box_output( $self, "ERROR|Неверные данные IP-адреса" ) unless $param->{ ip } =~ /^([0-9]{1,3}[\.]){3}[0-9]{1,3}$/;
		
	my ( $pass, $cashbox_type ) = $vars->db->sel1("
		SELECT CashPassword, CashboxType FROM Cashboxes_interceptors WHERE InterceptorIP = ?", $param->{ ip }
	);
	
	return cash_box_output( $self, "ERROR|IP-адрес не найден в БД или не установлен кассовый пароль" ) unless $pass;
	
	$vars->db->query("
		UPDATE Cashboxes_interceptors SET LastVersion = ?, LastUser = ?, LastUse = now() WHERE InterceptorIP = ?", {},
		$param->{ v }, $login, $param->{ ip }
	);
	
	return cash_box_output( $self, "OK|$pass|$surname $name $secname|$cashbox_type" );
}

sub cash_box_centers
# //////////////////////////////////////////////////
{
	my ( $self, $task, $id, $template, $slist, $authip, $clientip ) = @_;
	
	my $vars = $self->{'VCS::Vars'};
	
	my $login = $vars->getparam( 'login' ) || undef;
	
	return cash_box_output( $self, "" ) unless $login;
	
	my $centers_line = $vars->db->sel1("
		SELECT branches FROM Users WHERE Login = ?", $login
	);
	
	my $centers_array = $vars->db->selallkeys("
		SELECT BName FROM Branches WHERE ID in ($centers_line)"
	);
	
	return cash_box_output( $self, "" ) if @$centers_array == 0;
	
	my $centers_name = join ( '|', map { $_->{ BName } } @$centers_array );
	
	return cash_box_output( $self, $centers_name );
}

sub cash_box_vtype
# //////////////////////////////////////////////////
{
	my ( $self, $task, $id, $template, $slist, $authip, $clientip ) = @_;
	
	my $vars = $self->{'VCS::Vars'};
	
	my $center = $vars->getparam( 'center' ) || '';
	
	return cash_box_output( $self, "" ) unless $center;
	
	my $vtypes_array = $vars->db->selallkeys("
		SELECT VName, Centers FROM VisaTypes"
	);
		
	my $center_id = $vars->db->sel1("
		SELECT ID FROM Branches WHERE BName = ?", $center
	);
		
	my $vtypes = '';
	
	for ( @$vtypes_array ) {
	
		my %centers = map { $_ => 1 } split( /,/, $_->{ Centers } );

		$vtypes .= ( $vtypes ? '|' : '' ) . $_->{ VName } if exists $centers{ $center_id };
	};
	
	return cash_box_output( $self, $vtypes );
}

sub download_receipt
{
	my ( $self, $task, $id, $template ) = @_;
	
	my $vars = $self->{'VCS::Vars'};
	
	my $gconfig = $vars->getConfig( 'general' );
	
	my $file_id = $vars->getparam('rid') || undef;
	
	return cash_box_output( $self, "Ошибка параметров загрузки акта" ) unless $file_id;
	
	my $file = $vars->db->selallkeys("
		SELECT CRC, OriginalName FROM Cashboxes_receipt WHERE ID = ?", $file_id
	)->[0];
	
	return cash_box_output( $self, "Акт не найден в системе" ) unless $file->{ CRC };
   
	my $file_path = $gconfig->{ tmp_folder } . "cashbox_receipt/" . $file->{ CRC };

	my $content = '';
	
	open my $filefile, '<', $file_path or die;
	
	$content .= $_ while ( <$filefile> );

	close $filefile;
	
	print "HTTP/1.1 200 Ok\n";
	print "Content-type: application/x-www-form-urlencoded; name=\"" . $file->{ CRC } . "\"\n";
	print "Content-Disposition: attachment; filename=\"" . $file->{ OriginalName } . "\"\n";
	print "\n";
 
	print $content;
}

sub cash_box_upload
# //////////////////////////////////////////////////
{
	my ( $self, $task, $id, $template, $slist, $authip, $clientip ) = @_;

	my $vars = $self->{ 'VCS::Vars' };
	
	my $param = {};
	
	my $gconfig = $vars->getConfig( 'general' );
	
	$param->{ $_ } = ( $vars->getparam( $_ ) || 0 )
		for ( 'md5', 'appID', 'actnum', 'login', 'xerox', 'form', 'photo', 'print' );

        my $fobj = $vars->getparamfile('file');
	
	$vars->get_system->pheader( $vars );
		
	return print "ERROR|Неверный запрос на загрузку файла"
		unless $param->{ md5 } and $fobj and $param->{ appID } and $param->{ login };
           
        my $up_name = $gconfig->{ tmp_folder } . "cashbox_receipt/" . $param->{ md5 };
		
	my ( $file, $buffer );
	
	open $file, '>', $up_name or die;
	
	binmode $file;
	
	while ( my $bytesread = read( $fobj->{ file }, $buffer, 1024 ) ) {
	
		print $file $buffer;
		
		$buffer = "";
	} 
	
	close $file;  

	open my $file_check, '<', $up_name;
	
	my $md5file = md5_filecrc_with_secret_code( $file_check );
	
	close $file_check;
	
	if ( $md5file ne $param->{ md5 } ) { 
	   
		unlink $up_name;
               
		print "ERROR|Ошибка загрузки файла";
	}
	else {
		$vars->db->query("
			INSERT INTO Cashboxes_receipt (Login, AppID, ActNum, Upload,
			CRC, OriginalName, Xerox, Form, Photo, Print) 
			VALUES (?, ?, ?, CURDATE(), ?, ?, ?, ?, ?, ?)", {},
			$param->{ login }, $param->{ appID }, $param->{ actnum }, $param->{ md5 }, $fobj->{ filename },
			$param->{ xerox }, $param->{ form }, $param->{ photo }, $param->{ print }
		);
		
		print "OK|Файл загружен";
	}
}

sub cash_box_appinfo
# //////////////////////////////////////////////////
{
	my ( $self, $task, $id, $template, $slist, $authip, $clientip ) = @_;

	my $vars = $self->{ 'VCS::Vars' };
	
	my $gconfig = $vars->getConfig('general');
	
	my $param = {};
	
	$param->{ $_ } = ( $vars->getparam( $_ ) || '' ) for ( 'app', 'summ', 'crc' );
	
	my $request_check = "app=" . $param->{ app } . "&summ=" . $param->{ summ };
		
	my $md5 = uc( md5_crc_with_secret_code( $request_check, 'not_ord' ) );
	
	return cash_box_output( $self, "ERROR|Контрольная сумма запроса неверна" ) unless $md5 eq $param->{ crc };
	
	my ( $person_arr, $ord_num ) = ( {}, 0 );

	if ( length( $param->{ app } ) == 15 ) {

		$person_arr = $vars->db->selallkeys("
			SELECT ID, AppNum, LName, FName, MName FROM Appointments WHERE AppNum = ?", $param->{ app }
		)->[0];
	}
	elsif ( length( $param->{ app } ) == 9 ) {
	
		$person_arr = $vars->db->selallkeys("
			SELECT Appointments.ID, AppNum, Appointments.LName, Appointments.FName, Appointments.MName
			FROM Appointments
			JOIN AppData ON Appointments.ID = AppData.AppID
			WHERE AppData.PassNum = ? GROUP BY Appointments.ID", 
			$param->{ app }
		)->[0];
	}
	else {
		$person_arr = {};
	}
	
	if ( $person_arr->{ ID } ) {
	
		$ord_num = $vars->db->sel1("
			SELECT COUNT(ID) FROM Cashboxes_receipt WHERE AppID = ?", $person_arr->{ ID }
		) || 0;
	}
	
	my $person = $person_arr->{ LName } . ' ' . $person_arr->{ FName } . ' ' . $person_arr->{ MName };
	
	my $appnum = $person_arr->{ AppNum };
	
	my $appid = $person_arr->{ ID };

	my $summ_text = $vars->admfunc->sum_to_russtr( 'RUR', $param->{ summ } );
	
	my $vat = $param->{ summ } * $gconfig->{'VAT'} / ( 100 + $gconfig->{'VAT'} );
	
	my $vat_text = $vars->admfunc->sum_to_russtr( 'RUR', $vat );

	return cash_box_output( $self, "OK|$person|$appnum|$summ_text|$vat_text|$ord_num|$appid" );
}

sub cash_box_mandocpack
# //////////////////////////////////////////////////
{
	my ( $self, $task, $id, $template, $slist, $authip, $clientip ) = @_;
	
	my $vars = $self->{ 'VCS::Vars' };
	
	my $gconfig = $vars->getConfig('general');
	
	my $param = {};
	
	my $data = { direct_docpack => 1 };
	
	$param->{ $_ } = ( $vars->getparam( $_ ) || '' )
		for ( 'login', 'pass', 'moneytype', 'money', 'services', 'center', 'vtype', 'callback', 'rdate', 'crc', 'r' );

	my $request_check = "login=" . $param->{ login } . "&pass=" . $param->{ pass } .
		"&moneytype=" . $param->{ moneytype } . "&money=" . $param->{ money } . "&center=" . $param->{ center } .
		"&vtype=" . $param->{ vtype } . "&rdate=" . $param->{ rdate } . "&services=" . $param->{ services } .
		"&callback=" . $param->{ callback } . "&r=" . ( $param->{ r } ? '1' : '0' );

	my $md5 = uc( md5_crc_with_secret_code( $request_check, 'not_ord' ) );

	return cash_box_output( $self, "ERROR|Контрольная сумма запроса неверна" ) unless $md5 eq $param->{ crc };
	
	return cash_box_output( $self, "ERROR|Не установлена кассовая интеграция" )
		unless $param->{ callback } =~ /^([0-9]{1,3}[\.]){3}[0-9]{1,3}$/;
		
	my $center_id = undef;

	if ( $param->{ r } && ( length( $param->{ center } ) == 15 ) ) {
	
		$param->{ center } =~ /^(\d{3})/;
		
		my $center_id_line = $1;
		
		$center_id_line =~ s/^[0]+//;
	
		$center_id = $vars->db->sel1("
			SELECT ID FROM Branches WHERE ID = ?", $center_id_line
		);
	}
	elsif ( $param->{ r } && ( length( $param->{ center } ) == 9 ) ) {
	
		$center_id = $vars->db->sel1("
			SELECT Branches.ID
			FROM Appointments
			JOIN AppData ON Appointments.ID = AppData.AppID
			JOIN Branches ON Appointments.CenterID = Branches.ID
			WHERE AppData.PassNum = ? 
			ORDER BY ID DESC LIMIT 1",
			$param->{ center }
		);
	}
	else {
		$center_id = $vars->db->sel1("
			SELECT ID FROM Branches WHERE BName = ?", $param->{ center }
		);
	}
	
	return cash_box_output( $self, "ERROR|Данные записи не найдены" ) unless $center_id;
		
	$data->{ center } = $center_id;
	
	my $serv_hash = {};
		
	my $rate_date = $vars->get_system->now_date();
	
	$rate_date = "$3-$2-$1" if $param->{ rdate } =~ /(\d{2})\.(\d{2})\.(\d{4})/;
	
	$data->{ concilPaymentDate } = $rate_date;
	
	$data->{ ipdate } = $vars->get_system->now_date();
	
	$data->{ rate } = $vars->admfunc->getRate( $vars, $gconfig->{'base_currency'}, $rate_date, $center_id );
	
	( $data->{ vtype }, $data->{ vcat } ) = $vars->db->sel1("
		SELECT ID, category FROM VisaTypes WHERE VName = ?", $param->{ vtype }
	);
	
	$param->{ services } =~ s/,/./g;

	$serv_hash->{ $_ } += 1 for split( /\|/, $param->{ services } );

	my ( $urgent_docpack, $concil_index ) = ( 0, 0 );
	
	$data->{ applicants } = [];
	
	for ( keys %$serv_hash ) {
	
		if ( /^dhl=(.+)$/ ) {

			$data->{ shipsum } = $1;

			$data->{ $_ } = 1 for ( 'shipping' , 'newdhl' );
		}
		
		if ( /^insurance([^=]+?)=(.+)$/ ) {

			my ( $service, $price ) = ( "insurance_manual_service_$1", $2 );

			$data->{ $service } = $price;
		}
		
		if ( /^(service|service_urgent)$/ ) {
		
			for my $n ( 0..($serv_hash->{ $_ } - 1) ) {
			
				$data->{ applicants }->[ $n ]->{ Status } = 1;
			}
			
			$urgent_docpack += ( $_ eq 'service_urgent' ? 1 : 0 );
		}
		
		if ( /^(concil|concil_urg_r|concil_n|concil_n_age)$/ ) {
	
			for my $n ( 0..($serv_hash->{ $_ } - 1) ) {
		
				$data->{ applicants }->[ $concil_index + $n ]->{ Concil } = 0 if /^(concil|concil_urg_r)$/;
				
				$data->{ applicants }->[ $concil_index + $n ]->{ AgeCatA } = 1 if /^concil_n_age$/;
				
				$data->{ applicants }->[ $concil_index + $n ]->{ iNRes } = 1 if /^concil_n$/;
				
				$data->{ applicants }->[ $concil_index + $n ]->{ direct_concil } = 1;
			}
			
			$concil_index += $serv_hash->{ $_ };
			
			$urgent_docpack += ( $_ eq 'concil_urg_r' ? 1 : 0 );
		}

		$data->{ $_ } = $serv_hash->{ $_ } if /^(vipsrv|sms_status|anketasrv|transum|printsrv|photosrv|xerox)$/;
		
		$data->{ $_ } = $serv_hash->{ $_ } if /^(srv1|srv2|srv3|srv4|srv5|srv6|srv7|srv8|srv9)$/;
	}

	$data->{ urgent } = ( $urgent_docpack ? 1 : 0 );
	
	my ( undef, undef, $mandocpack_failserv ) = send_docpack( $self, undef, $param->{ moneytype }, $param->{ money }, $data,
		$param->{ login }, $param->{ pass }, $param->{ callback }, $param->{ r } );

	return cash_box_output( $self, "WARNING|$mandocpack_failserv" ) if $mandocpack_failserv;

	return cash_box_output( $self, "OK|Запрос получен" );
}

sub get_access_for_cashbox_payment
# //////////////////////////////////////////////////
{
	my $self = shift;
	
	my $vars = $self->{ 'VCS::Vars' };

	my $interceptor_ip = $vars->db->sel1("
		SELECT InterceptorIP FROM Cashboxes_interceptors WHERE ID = ?", $vars->get_session->{ interceptor }
	) || undef;
	
	return ( $interceptor_ip =~ /^([0-9]{1,3}[\.]){3}[0-9]{1,3}$/ ? 1 : 0 );
}

sub cashbox_payment_control
# //////////////////////////////////////////////////
{
	my ( $self, $task, $id, $template, $slist, $authip, $clientip ) = @_;
	
	my $vars = $self->{ 'VCS::Vars' };
	
	my $param = {};
	
	$param->{ $_ } = ( $vars->getparam( $_ ) || '' ) for ( 'login', 'p', 'docnum', 'ip' );
	
	return cash_box_output( $self, "ERROR|Ошибка параметров" )
		if !$param->{ login } or !$param->{ p } or !$param->{ docnum };
		
	$param->{ docnum } =~ s/[^\d]+//g;	

	my $statuses = $vars->db->selallkeys("	
		SELECT DocPack.ID, DocPackList.ID as DID, PStatus, Status, DocPackInfo.BankID, DocPackList.PassNum
		FROM DocPack
		JOIN DocPackInfo ON DocPack.ID = DocPackInfo.PackID
		JOIN DocPackList ON DocPackInfo.ID = DocPackList.PackInfoID
		WHERE DocPack.AgreementNo = ?",
		$param->{ docnum }
	);
	
	return cash_box_output( $self, "ERROR|Договор не найден по номеру" ) unless $statuses->[0]->{ ID };

	my $all_status_is_ok = 1;
	
	my $dpacklist_array = [];
	
	for ( @$statuses ) {
	
		$all_status_is_ok = 0 if $_->{ PStatus } == 1 or $_->{ Status } == 1;
		
		push( @$dpacklist_array, $_->{ DID } );
	}
	
	return cash_box_output( $self, "OK|Договор был оплачен" ) if $all_status_is_ok;	
	
	my ( $login, $name, $surname, $secname ) = $vars->db->sel1("
		SELECT Login, UserName, UserLName, UserSName
		FROM Users
		WHERE Login = ? AND Pass = ? AND
		(RoleID = 8 OR RoleID = 5 OR RoleID = 2)
		AND Locked = 0",
		$param->{ login }, $param->{ p }
	);

	return cash_box_output( $self, "ERROR|Неверный логин или пароль" ) unless $login;
	
	my ( $cashbox_id, $cashbox_name, $cashbox_report_name ) = $vars->db->sel1("
		SELECT ID, Name, ReportName FROM Cashboxes_interceptors WHERE InterceptorIP = ?",
		$param->{ ip }
	);
	
	$vars->db->query( "LOCK TABLES DocPack WRITE, DocPackList WRITE,
		Cashboxes_errors WRITE, DocCashbox WRITE, DocHistory WRITE" );
	
	$vars->db->query("
		UPDATE DocPack SET PStatus = 2 WHERE ID = ? AND PStatus = 1", {},
		$statuses->[0]->{ ID }
	);
	
	my $dpacklist = join( "','", @$dpacklist_array );

	$vars->db->query("
		UPDATE DocPackList SET Status = 2, SDate = now() WHERE ID IN ('$dpacklist') AND Status = 1"
	);
	
	# ////////////////////////////////
	
	my $cashbox_already = $vars->db->sel1("
		SELECT ID FROM DocCashbox WHERE DocPackID = ?",
		$statuses->[0]->{ ID }
	);
	
	$vars->db->query("
		INSERT INTO DocCashbox (DocPackID, PayDate, Login, Cashbox)
		VALUES (?, now(), ?, ?)", {},
		$statuses->[0]->{ ID }, $param->{ login }, $cashbox_report_name
	) unless $cashbox_already;
	
	my $dochistory_already = $vars->db->sel1("
		SELECT PassNum FROM DocHistory WHERE DocID = ?",
		$statuses->[0]->{ ID }
	);
	
	if ( !$dochistory_already ) {
	
		for ( @$statuses ) {
		
			$vars->db->query("
				INSERT INTO DocHistroy (DocID, PassNum, Login, HDate, StatusID, BankID, ActTime, AddInfo, ODuration, FPStatus)
				VALUES (?, ?, ?, now(), 2, ?, 0, '', 0, 0)", {},
				$_->{ ID }, $_->{ PassNum }, $param->{ login }, $param->{ BankID }
			);
		}
	}
	
	# ////////////////////////////////
	
	$vars->db->query("
		INSERT INTO Cashboxes_errors (Cashbox, CashboxID, Login, DocPackID, ErrorDate, Error)
		VALUES (?, ?, ?, ?, now(), ?)", {},
		$cashbox_name, $cashbox_id, $param->{ login }, $statuses->[0]->{ ID }, 'Сработал добиватель оплаты'
	);
	
	$vars->db->query( "UNLOCK TABLES" );
	
	return cash_box_output( $self, "OK|Договор переведён в статус оплаченный" );
}

sub cash_box_output
# //////////////////////////////////////////////////
{
	my ( $self, $msg ) = @_;
	
	my $vars = $self->{ 'VCS::Vars' };

	$vars->get_system->pheader( $vars );
	
	print $msg;
}

1;
